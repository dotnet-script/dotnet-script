using System.Collections.Generic;
using System.Linq;
using System.IO;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using System.Reflection;

namespace Dotnet.Script.DependencyModel.Runtime
{
    public class RuntimeDependencyResolver
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptEnvironment _scriptEnvironment;

        private readonly ScriptDependencyContextReader _dependencyContextReader;

        private readonly IRestorer _restorer;

        public RuntimeDependencyResolver(ScriptProjectProvider scriptProjectProvider, LogFactory logFactory, ScriptEnvironment scriptEnvironment, bool useRestoreCache)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptEnvironment = scriptEnvironment;
            _dependencyContextReader = new ScriptDependencyContextReader(logFactory);
            _restorer = CreateRestorer(logFactory, useRestoreCache);
        }

         public RuntimeDependencyResolver(LogFactory logFactory, bool useRestoreCache) : this(new ScriptProjectProvider(logFactory), logFactory, ScriptEnvironment.Default, useRestoreCache)
         {

         }

        private static IRestorer CreateRestorer(LogFactory logFactory, bool useRestoreCache)
        {
            var commandRunner = new CommandRunner(logFactory);
            if (useRestoreCache)
            {
                return new ProfiledRestorer(new CachedRestorer(new DotnetRestorer(commandRunner, logFactory),logFactory),logFactory);
            }
            else
            {
                return new ProfiledRestorer(new DotnetRestorer(commandRunner, logFactory),logFactory);
            }
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string scriptFile, string[] packageSources)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProjectForScriptFile(scriptFile);
            _restorer.Restore(pathToProjectFile, packageSources);
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");
            return GetDependenciesInternal(pathToAssetsFile);
        }

        public IEnumerable<RuntimeDependency> GetDependenciesForLibrary(string pathToLibrary)
        {
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(pathToLibrary), "obj", "project.assets.json");
            return GetDependenciesInternal(pathToAssetsFile);
        }

        public IEnumerable<RuntimeDependency> GetDependenciesForCode(string targetDirectory, ScriptMode scriptMode, string[] packageSources, string code = null)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProjectForRepl(code, Path.Combine(targetDirectory, scriptMode.ToString()), ScriptEnvironment.Default.TargetFramework);
            _restorer.Restore(pathToProjectFile, packageSources);
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");
            return GetDependenciesInternal(pathToAssetsFile);
        }

        private IEnumerable<RuntimeDependency> GetDependenciesInternal(string pathToAssetsFile)
        {
            var context = _dependencyContextReader.ReadDependencyContext(pathToAssetsFile);
            var result = new List<RuntimeDependency>();
            foreach (var scriptDependency in context.Dependencies)
            {
                var runtimeAssemblies = scriptDependency.RuntimeDependencyPaths.Select(rdp => new RuntimeAssembly(AssemblyName.GetAssemblyName(rdp), rdp)).ToList();
                var runtimeDependency = new RuntimeDependency(scriptDependency.Name, scriptDependency.Version,runtimeAssemblies, scriptDependency.NativeAssetPaths,scriptDependency.ScriptPaths);
                result.Add(runtimeDependency);
            }

            return result;
        }
    }
}