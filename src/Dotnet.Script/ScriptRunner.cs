using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
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

        readonly TextWriter _stderr;

        public ScriptRunner(TextWriter stderr)
        {
            _stderr = stderr ?? TextWriter.Null;
        }

        void Write(string s) => _stderr.Write(s);
        void WriteLine(string s) => _stderr.WriteLine(s);

        public async Task Execute<TReturn>(ScriptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.DebugMode)
            {
                WriteLine($"Using debug mode.");
                WriteLine($"Using configuration: {context.Configuration}");
            }

            if (!File.Exists(context.FilePath))
            {
                // TODO throw an exception instead
                WriteLine($"Couldn't find file '{context.FilePath}'");
                return;
            }

            var directory = Path.IsPathRooted(context.FilePath) ? Path.GetDirectoryName(context.FilePath) : Directory.GetCurrentDirectory();
            var runtimeContext = ProjectContext.CreateContextForEachTarget(directory).First();

            if (context.DebugMode)
            {
                WriteLine($"Found runtime context for '{runtimeContext.ProjectFile.ProjectFilePath}'");
            }

            var projectExporter = runtimeContext.CreateExporter(context.Configuration);
            var runtimeDependencies = new HashSet<string>();
            var projectDependencies = projectExporter.GetDependencies();

            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblies = projectDependency.RuntimeAssemblyGroups;

                foreach (var runtimeAssembly in runtimeAssemblies.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = runtimeAssembly.ResolvedPath;
                    if (context.DebugMode)
                    {
                        WriteLine($"Discovered runtime dependency for '{runtimeAssemblyPath}'");
                    }
                    runtimeDependencies.Add(runtimeAssemblyPath);
                }
            }

            var code = File.ReadAllText(context.FilePath);

            var opts = ScriptOptions.Default.
                AddImports(DefaultNamespaces).
                AddReferences(DefaultAssemblies).
                AddReferences(typeof(ScriptingHost).GetTypeInfo().Assembly).
                WithSourceResolver(new RemoteFileResolver(directory));

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var inheritedAssemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId).Where(x => x.FullName.ToLowerInvariant().StartsWith("system.") || x.FullName.ToLowerInvariant().StartsWith("mscorlib"));

            foreach (var inheritedAssemblyName in inheritedAssemblyNames)
            {
                if (context.DebugMode)
                {
                    WriteLine("Adding reference to an inherited dependency => " + inheritedAssemblyName.FullName);
                }
                var assembly = Assembly.Load(inheritedAssemblyName);
                opts = opts.AddReferences(assembly);
            }

            foreach (var runtimeDep in runtimeDependencies)
            {
                if (context.DebugMode)
                {
                    Console.WriteLine("Adding reference to a runtime dependency => " + runtimeDep);
                }
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep));
            }

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create<TReturn>(code, opts, typeof(ScriptingHost), loader);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diagnostic in diagnostics)
                {
                    Write("There is an error in the script.");
                    WriteLine(diagnostic.ToString());
                }
            }
            else
            {
                if (context.DebugMode)
                {
                    foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
                        WriteLine(diagnostic.ToString());
                }

                var host = new ScriptingHost
                {
                    ScriptDirectory = directory,
                    ScriptPath = context.FilePath,
                    ScriptArgs = context.ScriptArgs,
                    ScriptAssembly = script.GetScriptAssembly(loader)
                };

                var scriptResult = await script.RunAsync(host).ConfigureAwait(false);
                if (scriptResult.Exception != null)
                {
                    Write("Script execution resulted in an exception.");
                    WriteLine(scriptResult.Exception.Message);
                    WriteLine(scriptResult.Exception.StackTrace);
                }
            }
        }
    }
}