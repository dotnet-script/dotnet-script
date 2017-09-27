using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver 
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly DependencyContextProvider _dependencyContextProvider;
        private readonly Action<bool, string> _logger;
        private readonly Lazy<ICompilationAssemblyResolver[]> _assemblyResolvers;

        public CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, DependencyContextProvider dependencyContextProvider, Action<bool, string> logger)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _dependencyContextProvider = dependencyContextProvider;
            _logger = logger;
            _assemblyResolvers = new Lazy<ICompilationAssemblyResolver[]>(GetCompilationAssemblyResolvers);
        }

        public CompilationDependencyResolver(Action<bool, string> logger) 
            : this
            (
                new ScriptProjectProvider(logger), 
                new DependencyContextProvider(CreateRestorers(logger), logger), 
                logger
            )
        {            
        }

        private static IRestorer[] CreateRestorers(Action<bool, string> logger)
        {
            var commandRunner = new CommandRunner(logger);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logger), new NuGetRestorer(commandRunner, logger) };
        }

        public static CompilationDependencyResolver Create(Action<bool, string> logger)
        {
            var commandRunner = new CommandRunner(logger);
            var restorers = new IRestorer[] { new DotnetRestorer(commandRunner, logger), new NuGetRestorer(commandRunner, logger)};
            var dependencyContextProvider = new DependencyContextProvider(restorers, logger);
            return new CompilationDependencyResolver(ScriptProjectProvider.Create(logger), dependencyContextProvider,logger);
        }

        public IEnumerable<string> GetDependencies(string targetDirectory, bool enableScriptNugetReferences, string defaultTargetFramework = "net46")
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, defaultTargetFramework,
                enableScriptNugetReferences);

            if (pathToProjectFile == null)
            {
                return Array.Empty<string>();
            }

            var dependencyContext = _dependencyContextProvider.GetDependencyContext(pathToProjectFile);

            var resolvedReferencePaths = new HashSet<string>();
            
            var compileLibraries = dependencyContext.CompileLibraries;

            foreach (var compilationLibrary in compileLibraries)
            {                
                _logger.Verbose($"Resolving compilation reference paths for {compilationLibrary.Name}");
                var referencePaths = ResolveReferencePaths(compilationLibrary);
                foreach (var referencePath in referencePaths)
                {
                    resolvedReferencePaths.Add(referencePath);
                }
            }
            return resolvedReferencePaths;
        }

        private IEnumerable<string> ResolveReferencePaths(CompilationLibrary compilationLibrary)
        {
            
            if (compilationLibrary.Assemblies.Any(a => a.EndsWith("_._")))
            {
                return Array.Empty<string>();
            }

            var referencePaths = compilationLibrary.ResolveReferencePaths(_assemblyResolvers.Value).ToArray();

            foreach (var referencePath in referencePaths)
            {
                _logger.Verbose($"{compilationLibrary.Name} => {referencePath}");
            }

            return referencePaths;                        
        }

        private ICompilationAssemblyResolver[] GetCompilationAssemblyResolvers()
        {
            List<ICompilationAssemblyResolver> resolvers = new List<ICompilationAssemblyResolver>();
            resolvers.Add(new AppBaseCompilationAssemblyResolver());
            resolvers.Add(new ReferenceAssemblyPathResolver());
            resolvers.Add(CreatePackageResolver(RuntimeHelper.GetPathToNuGetFallbackFolder()));
            resolvers.Add(CreatePackageResolver(RuntimeHelper.GetPathToGlobalPackagesFolder()));
            return resolvers.ToArray();
        }

        private PackageCompilationAssemblyResolver CreatePackageResolver(string nugetGetPackageDirectory)
        {
            _logger.Verbose($"Creating {nameof(PackageCompilationAssemblyResolver)} for target path: {nugetGetPackageDirectory}");
            return new PackageCompilationAssemblyResolver(nugetGetPackageDirectory);
        }

    }
}