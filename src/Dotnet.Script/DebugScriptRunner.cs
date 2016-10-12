using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script
{
    public class DebugScriptRunner : ScriptRunner
    {
        public DebugScriptRunner(TextWriter stderr) : base(stderr)
        {
        }

        protected override Action<string> VerboseWriteLine => WriteLine;

        public override Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            WriteLine($"Using debug mode.");
            WriteLine($"Using configuration: {context.Configuration}");

            var compilationContext = GetCompilationContext<TReturn>(context);

            foreach (var diagnostic in compilationContext.Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning))
            {
                WriteLine(diagnostic.ToString());
            }

            foreach (var syntaxTree in compilationContext.Compilation.SyntaxTrees)
            {
                var encodingField = syntaxTree.GetType().GetField("_encodingOpt", BindingFlags.Instance | BindingFlags.NonPublic);
                encodingField.SetValue(syntaxTree, Encoding.UTF8);

                var lazyTextField = syntaxTree.GetType().GetField("_lazyText", BindingFlags.Instance | BindingFlags.NonPublic);
                lazyTextField.SetValue(syntaxTree, compilationContext.SourceText);
            }

            using (var peStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitOptions = new EmitOptions().WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                var emitResult = compilationContext.Compilation.Emit(peStream, pdbStream, null, null, null, emitOptions);

                if (emitResult.Success)
                {
                    var getBoundReferenceManagerMethod = compilationContext.Compilation.GetType().GetMethod("GetBoundReferenceManager", BindingFlags.Instance | BindingFlags.NonPublic);
                    var referenceManager = getBoundReferenceManagerMethod.Invoke(compilationContext.Compilation, null);

                    var getReferencedAssembliesMethod = referenceManager.GetType().GetMethod("GetReferencedAssemblies", BindingFlags.Instance | BindingFlags.NonPublic);
                    var referencedAssemblies = getReferencedAssembliesMethod.Invoke(referenceManager, null) as IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>>;

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

                    var loaderAseemblyLoadMethod = compilationContext.Loader.GetType().GetMethod("LoadAssemblyFromStream", BindingFlags.Instance | BindingFlags.NonPublic);
                    var assembly = loaderAseemblyLoadMethod.Invoke(compilationContext.Loader, new[] { peStream, pdbStream }) as Assembly;
                    compilationContext.Host.ScriptAssembly = assembly;

                    var entryPoint = compilationContext.Compilation.GetEntryPoint(default(CancellationToken));
                    var entryPointType = assembly.GetType(entryPoint.ContainingType.MetadataName, true, false).GetTypeInfo();
                    var method = entryPointType.GetDeclaredMethod(entryPoint.MetadataName);

                    var resultTask = (Task<TReturn>)method.Invoke(null, new[] { new object[2] { compilationContext.Host, null } });
                    return resultTask;
                }
            }

            return Task.FromResult(default(TReturn));
        }
    }
}