using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script
{
    public class Program
    {
        private static IEnumerable<Assembly> _assemblies = new[]
{
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        private static IEnumerable<string> _namespaces = new[]
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic"
        };

        public static void Main(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            var file = commandLineApplication.Argument("script", "Path to CSX script");
            var config = commandLineApplication.Option("-c |--configuration <configuration>", "Configuration to use. Defaults to 'Release'", CommandOptionType.SingleValue);
            var debugMode = commandLineApplication.Option("-d |--debug", "Enables debug output.", CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    RunScript(file.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue());
                }
                return 0;
            });

            commandLineApplication.Execute(args);
        }

        private static void RunScript(string file, string config, bool debugMode)
        {
            if (debugMode)
            {
                Console.WriteLine($"Using debug mode.");
                Console.WriteLine($"Using configuration: {config}");
            }

            if (!File.Exists(file))
            {
                Console.WriteLine($"Couldn't find file '{file}'");
                return;
            }

            var directory = Path.IsPathRooted(file) ? Path.GetDirectoryName(file) : Directory.GetCurrentDirectory();
            var runtimeContext = ProjectContext.CreateContextForEachTarget(directory).First();

            if (debugMode)
            {
                Console.WriteLine($"Found runtime context for '{runtimeContext.ProjectFile.ProjectFilePath}'");
            }

            var projectExporter = runtimeContext.CreateExporter(config);
            var runtimeDependencies = new HashSet<string>();
            var projectDependencies = projectExporter.GetDependencies();

            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblies = projectDependency.RuntimeAssemblyGroups;

                foreach (var runtimeAssembly in runtimeAssemblies.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = runtimeAssembly.ResolvedPath;
                    if (debugMode)
                    {
                        Console.WriteLine($"Discovered runtime dependency for '{runtimeAssemblyPath}'");
                    }
                    runtimeDependencies.Add(runtimeAssemblyPath);
                }
            }

            var code = File.ReadAllText(file);

            var opts = ScriptOptions.Default.
                AddImports(_namespaces).
                AddReferences(_assemblies).
                AddReferences(typeof(ScriptingHost).GetTypeInfo().Assembly).
                WithSourceResolver(new RemoteFileResolver());

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var assemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId);

            foreach (var assemblyName in assemblyNames)
            {
                if (debugMode)
                {
                    Console.WriteLine("Adding reference to a default dependency => " + assemblyName.FullName);
                }
                var assembly = Assembly.Load(assemblyName);
                opts = opts.AddReferences(assembly);
            }

            foreach (var runtimeDep in runtimeDependencies)
            {
                if (debugMode)
                {
                    Console.WriteLine("Adding reference to a runtime dependency => " + runtimeDep);
                }
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep));
            }

            var script = CSharpScript.Create(code, opts, typeof(ScriptingHost));
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any())
            {
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.GetMessage());
                }
            }
            else
            {
                var scriptResult = script.RunAsync(new ScriptingHost()).Result;
                if (scriptResult.Exception != null)
                {
                    Console.Write("Script execution resulted in an exception.");
                    Console.WriteLine(scriptResult.Exception.Message);
                    Console.WriteLine(scriptResult.Exception.StackTrace);
                }
            }
        }
    }
}