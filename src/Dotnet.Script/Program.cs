using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
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
            if (args.Length == 0 || string.Equals(args[0], "-help", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: dotnet script {PATH}");
                return;
            }

            var file = args[0];
            if (!File.Exists(file))
            {
                Console.WriteLine($"Couldn't find file '{file}'");
                return;
            }

            var directory = Path.IsPathRooted(args[0]) ? Path.GetDirectoryName(file) : Directory.GetCurrentDirectory();

            var runtimeContext = ProjectContext.CreateContextForEachTarget(directory).First();
            Console.WriteLine($"Found runtime context for '{runtimeContext.ProjectFile.ProjectFilePath}'");

            var projectExporter = runtimeContext.CreateExporter("Debug");
            var runtimeDependencies = new HashSet<string>();
            var projectDependencies = projectExporter.GetDependencies();

            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblies = projectDependency.RuntimeAssemblyGroups;

                foreach (var runtimeAssembly in runtimeAssemblies.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = runtimeAssembly.ResolvedPath;
                    Console.WriteLine($"Found runtime dependency at context for '{runtimeAssemblyPath}'");
                    runtimeDependencies.Add(runtimeAssemblyPath);
                }
            }

            var code = File.ReadAllText(file);

            var opts = ScriptOptions.Default.
                AddImports(_namespaces).
                AddReferences(_assemblies).
                AddReferences(typeof(ScriptingHost).GetTypeInfo().Assembly);

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var assemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId);

            foreach (var assemblyName in assemblyNames)
            {
                Console.WriteLine("Inherited from dotnet script => " + assemblyName.FullName);
                var assembly = Assembly.Load(assemblyName);
                opts = opts.AddReferences(assembly);
            }

            foreach (var runtimeDep in runtimeDependencies)
            {
                Console.WriteLine("Runtime dep => " + runtimeDep);
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep));
            }

            var script = CSharpScript.Create(code, opts, typeof(ScriptingHost));
            var c = script.GetCompilation();

            var scriptResult = script.RunAsync(new ScriptingHost()).Result;

            if (scriptResult.Exception != null)
            {
                Console.Write("Script execution resulted in an exception.");
                Console.WriteLine(scriptResult.Exception.Message);
                Console.WriteLine(scriptResult.Exception.StackTrace);
            }
        }
    }

    public class ScriptingHost
    {
    }
}