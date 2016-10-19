using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script
{
    public class ScriptRunner
    {
        private static readonly IEnumerable<Assembly> DefaultAssemblies = new[]
        {
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        private static readonly IEnumerable<string> DefaultNamespaces = new[]
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic"
        };

        private readonly TextWriter _stderr;

        public ScriptRunner(TextWriter stderr)
        {
            _stderr = stderr ?? TextWriter.Null;
        }

        protected void Write(string s) => _stderr.Write(s);
        protected void WriteLine(string s) => _stderr.WriteLine(s);

        protected virtual Action<string> VerboseWriteLine => s => { };

        protected ScriptCompilationContext<TReturn> GetCompilationContext<TReturn>(ScriptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var runtimeContext = ProjectContext.CreateContextForEachTarget(context.WorkingDirectory).First();

            VerboseWriteLine($"Found runtime context for '{runtimeContext.ProjectFile.ProjectFilePath}'");

            var projectExporter = runtimeContext.CreateExporter(context.Configuration);
            var runtimeDependencies = new HashSet<string>();
            var projectDependencies = projectExporter.GetDependencies();

            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblies = projectDependency.RuntimeAssemblyGroups;

                foreach (var runtimeAssembly in runtimeAssemblies.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = runtimeAssembly.ResolvedPath;
                    VerboseWriteLine($"Discovered runtime dependency for '{runtimeAssemblyPath}'");
                    runtimeDependencies.Add(runtimeAssemblyPath);
                }
            }

            var opts = ScriptOptions.Default.
                AddImports(DefaultNamespaces).
                AddReferences(DefaultAssemblies).
                WithSourceResolver(new RemoteFileResolver(context.WorkingDirectory));

            if (!string.IsNullOrWhiteSpace(context.FilePath))
            {
                opts = opts.WithFilePath(context.FilePath);
            }

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var inheritedAssemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId).Where(x => 
            x.FullName.ToLowerInvariant().StartsWith("system.") ||
            x.FullName.ToLowerInvariant().StartsWith("microsoft.codeanalysis") ||
            x.FullName.ToLowerInvariant().StartsWith("mscorlib"));

            foreach (var inheritedAssemblyName in inheritedAssemblyNames)
            {
                VerboseWriteLine("Adding reference to an inherited dependency => " + inheritedAssemblyName.FullName);
                var assembly = Assembly.Load(inheritedAssemblyName);
                opts = opts.AddReferences(assembly);
            }

            foreach (var runtimeDep in runtimeDependencies)
            {
                VerboseWriteLine("Adding reference to a runtime dependency => " + runtimeDep);
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep));
            }

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create<TReturn>(context.Code.ToString(), opts, typeof(InteractiveScriptGlobals), loader);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diagnostic in diagnostics)
                {
                    WriteLine(diagnostic.ToString());
                }

                throw new CompilationErrorException("Script compilation failed due to one or more errors.",
                                                    diagnostics);
            }

            var host = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);
            foreach (var arg in context.Args)
            {
                host.Args.Add(arg);
            }

            return new ScriptCompilationContext<TReturn>(script, host, context.Code, loader);
        }

        public virtual async Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            var compilationContext = GetCompilationContext<TReturn>(context);

            var scriptResult = await compilationContext.Script.RunAsync(compilationContext.Host).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        protected TReturn ProcessScriptState<TReturn>(ScriptState<TReturn> scriptState)
        {
            if (scriptState.Exception != null)
            {
                Write("Script execution resulted in an exception.");
                WriteLine(scriptState.Exception.Message);
                WriteLine(scriptState.Exception.StackTrace);
            }

            return scriptState.ReturnValue;
        }
    }
}