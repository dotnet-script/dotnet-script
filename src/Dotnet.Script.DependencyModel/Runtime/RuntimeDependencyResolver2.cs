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
    public class RuntimeDependencyResolver2
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly bool _useRestoreCache;

        private readonly ScriptDependencyContextReader _dependencyContextReader;

        private readonly IRestorer _restorer;

        public RuntimeDependencyResolver2(ScriptProjectProvider scriptProjectProvider, LogFactory logFactory, ScriptEnvironment scriptEnvironment, bool useRestoreCache)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptEnvironment = scriptEnvironment;
            _useRestoreCache = useRestoreCache;
            _dependencyContextReader = new ScriptDependencyContextReader(logFactory);
            _restorer = CreateRestorer(logFactory, useRestoreCache);
        }

         public RuntimeDependencyResolver2(LogFactory logFactory, bool useRestoreCache) : this(new ScriptProjectProvider(logFactory), logFactory, ScriptEnvironment.Default, useRestoreCache)
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

        public IEnumerable<RuntimeDependency> GetDependencies(string scriptFile, string[] packagesSources)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProjectForScriptFile(scriptFile);
            return GetDependenciesInternal(pathToProjectFile, packagesSources);
        }



        private IEnumerable<RuntimeDependency> GetDependenciesInternal(string pathToProjectFile, string[] packageSources)
        {
            // TODO: base this on pathToAssetsFile?
            var fullpath = Path.GetFullPath(pathToProjectFile);
            _restorer.Restore(pathToProjectFile, packageSources);
            var pathToAssetsFile = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");
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