using System;
using System.Collections.Generic;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Parsing;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel
{
    public interface IScriptDependencyResolver
    {
        IEnumerable<ResolvedDependency> GetDependencies(string targetDirectory);
    }

    public class ScriptDependencyResolver : IScriptDependencyResolver
    {
        private readonly IScriptProjectProvider _scriptProjectProvider;
        private readonly IDependencyContextProvider _dependencyContextProvider;
        private readonly IDependencyResolver _dependencyResolver;

        public ScriptDependencyResolver(IScriptProjectProvider scriptProjectProvider, IDependencyContextProvider dependencyContextProvider, IDependencyResolver dependencyResolver)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _dependencyContextProvider = dependencyContextProvider;
            _dependencyResolver = dependencyResolver;
        }

        public IEnumerable<ResolvedDependency> GetDependencies(string targetDirectory)
        {
            var projectFile = _scriptProjectProvider.CreateProject(targetDirectory);
            var dependencyContext = _dependencyContextProvider.GetDependencyContext(projectFile);
            return _dependencyResolver.GetDependencies(dependencyContext);
        }

        public static ScriptDependencyResolver CreateRuntimeResolver(Action<bool, string> logAction)
        {
            return new ScriptDependencyResolver(new ScriptProjectProvider(new ScriptParser(logAction), logAction),
                new DependencyContextProvider(logAction),
                new RuntimeDependencyResolver(new DependencyPathResolver(logAction), logAction));
        }
    }
}