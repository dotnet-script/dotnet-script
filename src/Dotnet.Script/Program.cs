using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
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

        const string DebugFlagShort = "-d";
        const string DebugFlagLong = "--debug";

        public static int Main(string[] args)
        {
            try
            {
                return Wain(args);
            }
            catch (Exception e)
            {
                // Be verbose (stack trace) in debug mode otherwise brief
                var error = args.Any(arg => arg == DebugFlagShort
                                         || arg == DebugFlagLong)
                          ? e.ToString()
                          : e.GetBaseException().Message;
                Console.Error.WriteLine(error);
                return 0xbad;
            }
        }

        private static int Wain(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            var file = commandLineApplication.Argument("script", "Path to CSX script");

            var scriptArgs = commandLineApplication.Option("-a |--arg <args>", "Arguments to pass to a script. Multiple values supported", CommandOptionType.MultipleValue);
            var config = commandLineApplication.Option("-c |--configuration <configuration>", "Configuration to use. Defaults to 'Release'", CommandOptionType.SingleValue);
            var debugMode = commandLineApplication.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    RunScript(file.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(), scriptArgs.HasValue() ? scriptArgs.Values : new List<string>());
                }
                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static void RunScript(string file, string config, bool debugMode, List<string> scriptArgs)
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
                WithSourceResolver(new RemoteFileResolver(directory));

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var assemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId).Where(x => x.FullName.ToLowerInvariant().StartsWith("system.") || x.FullName.ToLowerInvariant().StartsWith("mscorlib"));

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

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create(code, opts, typeof(ScriptingHost), loader);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any())
            {
                foreach (var diagnostic in diagnostics)
                {
                    Console.Write("There is an error in the script.");
                    Console.WriteLine(diagnostic.GetMessage());
                }
            }
            else
            {
                var host = new ScriptingHost
                {
                    ScriptDirectory = directory,
                    ScriptPath = file,
                    ScriptArgs = scriptArgs,
                    ScriptAssembly = script.GetScriptAssembly(loader)
                };

                var scriptResult = script.RunAsync(host).Result;
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