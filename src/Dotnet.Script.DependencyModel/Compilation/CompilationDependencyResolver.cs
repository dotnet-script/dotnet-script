using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver
    {
        private readonly Logger _logger;
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptDependencyContextReader _scriptDependencyContextReader;
        private readonly IRestorer _restorer;

        public CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyContextReader scriptDependencyContextReader, LogFactory logFactory)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyContextReader = scriptDependencyContextReader;
            _restorer = CreateRestorer(logFactory);
        }

        public CompilationDependencyResolver(LogFactory logFactory)
            : this
            (
                new ScriptProjectProvider(logFactory),
                new ScriptDependencyContextReader(logFactory),
                logFactory
            )
        {
        }

        public IEnumerable<CompilationDependency> GetDependencies(string targetDirectory, IEnumerable<string> scriptFiles, bool enableScriptNugetReferences, string defaultTargetFramework = "net46")
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, scriptFiles,defaultTargetFramework, enableScriptNugetReferences);
            _restorer.Restore(pathToProjectFile, packageSources: Array.Empty<string>());
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");
            var dependencyContext = _scriptDependencyContextReader.ReadDependencyContext(pathToAssetsFile);
            var result = new List<CompilationDependency>();
            foreach (var scriptDependency in dependencyContext.Dependencies)
            {
                var compilationDependency = new CompilationDependency(scriptDependency.Name, scriptDependency.Version, scriptDependency.CompileTimeDependencyPaths, scriptDependency.ScriptPaths);
                result.Add(compilationDependency);
            }
            return result;
        }

        private static IRestorer CreateRestorer(LogFactory logFactory)
        {
            var commandRunner = new CommandRunner(logFactory);
            return new ProfiledRestorer(new DotnetRestorer(commandRunner, logFactory),logFactory);
        }
    }
}