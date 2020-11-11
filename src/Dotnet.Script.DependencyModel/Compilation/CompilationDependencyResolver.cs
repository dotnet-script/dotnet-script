using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptDependencyContextReader _scriptDependencyContextReader;
        private readonly ICompilationReferenceReader _compilationReferenceReader;

        private readonly IRestorer _restorer;

        public CompilationDependencyResolver(LogFactory logFactory) : this(new ScriptProjectProvider(logFactory), new ScriptDependencyContextReader(logFactory), new CompilationReferencesReader(logFactory), logFactory)
        {
        }

        public CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyContextReader scriptDependencyContextReader, ICompilationReferenceReader compilationReferenceReader, LogFactory logFactory)
        {
            _scriptProjectProvider = scriptProjectProvider;
            this._scriptDependencyContextReader = scriptDependencyContextReader;
            _compilationReferenceReader = compilationReferenceReader;
            _restorer = CreateRestorer(logFactory);
        }

        public IEnumerable<CompilationDependency> GetDependencies(string targetDirectory, IEnumerable<string> scriptFiles, bool enableScriptNugetReferences, string defaultTargetFramework = "net46")
        {
            var projectFileInfo = _scriptProjectProvider.CreateProject(targetDirectory, scriptFiles, defaultTargetFramework, enableScriptNugetReferences);
            _restorer.Restore(projectFileInfo, packageSources: Array.Empty<string>());
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(projectFileInfo.Path), "obj", "project.assets.json");
            var dependencyContext = _scriptDependencyContextReader.ReadDependencyContext(pathToAssetsFile);
            var result = new List<CompilationDependency>();
            foreach (var scriptDependency in dependencyContext.Dependencies)
            {
                var compilationDependency = new CompilationDependency(scriptDependency.Name, scriptDependency.Version, scriptDependency.CompileTimeDependencyPaths, scriptDependency.ScriptPaths);
                result.Add(compilationDependency);
            }

            // On .Net Core, we need to fetch the compilation references for framework assemblies separately.
            if (defaultTargetFramework.StartsWith("netcoreapp3", StringComparison.InvariantCultureIgnoreCase) ||
                defaultTargetFramework.StartsWith("net5", StringComparison.InvariantCultureIgnoreCase))
            {
                var compilationreferences = _compilationReferenceReader.Read(projectFileInfo);
                result.Add(new CompilationDependency("Dotnet.Script.Default.Dependencies", "99.0", compilationreferences.Select(cr => cr.Path).ToArray(), Array.Empty<string>()));
            }

            return result;
        }

        private static IRestorer CreateRestorer(LogFactory logFactory)
        {
            var commandRunner = new CommandRunner(logFactory);
            return new ProfiledRestorer(new DotnetRestorer(commandRunner, logFactory), logFactory);
        }
    }
}