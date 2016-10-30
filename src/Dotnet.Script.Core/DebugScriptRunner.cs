using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Dotnet.Script.Core
{
    public class DebugScriptRunner : ScriptRunner
    {
        public DebugScriptRunner(ScriptCompiler scriptCompiler, ScriptLogger logger) : base(scriptCompiler, logger)
        {
        }

        public override Task<TReturn> Execute<TReturn, THost>(ScriptContext context, THost host)
        {
            Logger.Log("Using debug mode.");
            Logger.Log($"Using configuration: {context.Configuration}");

            var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);

            var compilation = compilationContext.Script.GetCompilation();
            foreach (var diagnostic in compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning))
            {
                Logger.Log(diagnostic.ToString());
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Compilers/CSharp/Portable/Syntax/CSharpSyntaxTree.ParsedSyntaxTree.cs#L19
                var encodingField = syntaxTree.GetType().GetField("_encodingOpt", BindingFlags.Instance | BindingFlags.NonPublic);
                encodingField.SetValue(syntaxTree, Encoding.UTF8);

                // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Compilers/CSharp/Portable/Syntax/CSharpSyntaxTree.ParsedSyntaxTree.cs#L21
                var lazyTextField = syntaxTree.GetType().GetField("_lazyText", BindingFlags.Instance | BindingFlags.NonPublic);
                lazyTextField.SetValue(syntaxTree, compilationContext.SourceText);
            }

            using (var peStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitOptions = new EmitOptions().WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                var emitResult = compilation.Emit(peStream, pdbStream, null, null, null, emitOptions);

                if (emitResult.Success)
                {
                    // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Compilers/Core/Portable/Compilation/Compilation.cs#L478
                    var referenceManager = compilation.Invoke<object>("GetBoundReferenceManager", BindingFlags.NonPublic);

                    var referencedAssemblies =
                        // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Compilers/Core/Portable/ReferenceManager/CommonReferenceManager.State.cs#L34
                        referenceManager.Invoke<IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>>>("GetReferencedAssemblies", BindingFlags.NonPublic);

                    foreach (var referencedAssembly in referencedAssemblies)
                    {
                        var path = (referencedAssembly.Key as PortableExecutableReference)?.FilePath;
                        if (path != null)
                        {
                            compilationContext.Loader.RegisterDependency(referencedAssembly.Value.Identity, path);
                        }
                    }

                    peStream.Position = 0;
                    pdbStream.Position = 0;

                    var assembly =
                        // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Scripting/Core/Hosting/AssemblyLoader/InteractiveAssemblyLoader.cs#L111
                        compilationContext.Loader.Invoke<Stream, Stream, Assembly>(
                            "LoadAssemblyFromStream", BindingFlags.NonPublic,
                            peStream, pdbStream);

                    var entryPoint = compilation.GetEntryPoint(default(CancellationToken));
                    var entryPointType = assembly.GetType(entryPoint.ContainingType.MetadataName, true, false).GetTypeInfo();
                    var resultTask =
                        entryPointType.
                            GetDeclaredMethod(entryPoint.MetadataName).
                            Invoke<object[], Task<TReturn>>(
                                (object)null, // static invocation
                                new object[] { host, null });

                    return resultTask;
                }
            }

            return Task.FromResult(default(TReturn));
        }
    }
}