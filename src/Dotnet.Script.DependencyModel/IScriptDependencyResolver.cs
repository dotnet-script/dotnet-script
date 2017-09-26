using System;
using System.Collections.Generic;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Parsing;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.Runtime;

namespace Dotnet.Script.DependencyModel
{
    public interface IScriptDependencyResolver
    {
        IEnumerable<RuntimeDependency> GetDependencies(string targetDirectory);
    }

    public class ScriptDependencyResolver : IScriptDependencyResolver
    {
        private readonly IScriptProjectProvider _scriptProjectProvider;
        private readonly IDependencyContextProvider _dependencyContextProvider;
        private readonly IRuntimeDependencyResolver _runtimeDependencyResolver;

        private ScriptDependencyResolver(IScriptProjectProvider scriptProjectProvider, IDependencyContextProvider dependencyContextProvider, IRuntimeDependencyResolver runtimeDependencyResolver)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _dependencyContextProvider = dependencyContextProvider;
            _runtimeDependencyResolver = runtimeDependencyResolver;
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string targetDirectory)
        {
            var projectFile = _scriptProjectProvider.CreateProject(targetDirectory, "netcoreapp2.0");
            var dependencyContext = _dependencyContextProvider.GetDependencyContext(projectFile);
            return _runtimeDependencyResolver.GetDependencies(dependencyContext);
        }

        public static ScriptDependencyResolver CreateRuntimeResolver(Action<bool, string> logAction)
        {
            var restorers = new IRestorer[] {new DotnetRestorer(new CommandRunner(logAction), logAction)};

            return new ScriptDependencyResolver(ScriptProjectProvider.Create(logAction),
                new DependencyContextProvider(restorers, logAction),
                new RuntimeDependencyResolver(new DependencyPathResolver(logAction), logAction));
        }       
    }
}