using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script
{
    public class ScriptCompiler
    {
        private readonly ScriptLogger _logger;

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

        public ScriptCompiler(ScriptLogger logger)
        {
            _logger = logger;
        }

        public ScriptCompilationContext<TReturn> CreateCompilationContext<TReturn, THost>(ScriptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var runtimeContext = ProjectContext.CreateContextForEachTarget(context.WorkingDirectory).First();

            _logger.Verbose($"Found runtime context for '{runtimeContext.ProjectFile.ProjectFilePath}'");

            var projectExporter = runtimeContext.CreateExporter(context.Configuration);
            var runtimeDependencies = new HashSet<string>();
            var projectDependencies = projectExporter.GetDependencies();

            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblies = projectDependency.RuntimeAssemblyGroups;

                foreach (var runtimeAssembly in runtimeAssemblies.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = runtimeAssembly.ResolvedPath;
                    _logger.Verbose($"Discovered runtime dependency for '{runtimeAssemblyPath}'");
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
                _logger.Verbose("Adding reference to an inherited dependency => " + inheritedAssemblyName.FullName);
                var assembly = Assembly.Load(inheritedAssemblyName);
                opts = opts.AddReferences(assembly);
            }

            foreach (var runtimeDep in runtimeDependencies)
            {
                _logger.Verbose("Adding reference to a runtime dependency => " + runtimeDep);
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep));
            }

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create<TReturn>(context.Code.ToString(), opts, typeof(THost), loader);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diagnostic in diagnostics)
                {
                    _logger.Log(diagnostic.ToString());
                }

                throw new CompilationErrorException("Script compilation failed due to one or more errors.",
                    diagnostics);
            }

            return new ScriptCompilationContext<TReturn>(script, context.Code, loader);
        }
    }
}