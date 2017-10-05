using System;
using System.Collections.Generic;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
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
        private readonly ScriptDependencyInfoProvider _scriptDependencyInfoProvider;
        private readonly Action<bool, string> _logger;

        private CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyInfoProvider scriptDependencyInfoProvider,Action<bool, string> logger)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyInfoProvider = scriptDependencyInfoProvider;
            _logger = logger;
        }

        public CompilationDependencyResolver(Action<bool, string> logger) 
            : this
            (
                new ScriptProjectProvider(logger), 
                new ScriptDependencyInfoProvider(CreateRestorers(logger), logger),
                logger
            )
        { }
        
        private static IRestorer[] CreateRestorers(Action<bool, string> logger)
        {
            var commandRunner = new CommandRunner(logger);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logger), new NuGetRestorer(commandRunner, logger) };
        }
        
        public IEnumerable<string> GetDependencies(string targetDirectory, bool enableScriptNugetReferences, string defaultTargetFramework = "net46")
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, defaultTargetFramework,
                enableScriptNugetReferences);

            if (pathToProjectFile == null)
            {
                return Array.Empty<string>();
            }

            var dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectFile);

            var dependencyContext = dependencyInfo.DependencyContext;

            var compilationAssemblyResolvers = GetCompilationAssemblyResolvers(dependencyInfo.NugetPackageFolders);
           
            var resolvedReferencePaths = new HashSet<string>();
            
            var compileLibraries = dependencyContext.CompileLibraries;

            foreach (var compilationLibrary in compileLibraries)
            {                
                _logger.Verbose($"Resolving compilation reference paths for {compilationLibrary.Name}");
                var referencePaths = ResolveReferencePaths(compilationLibrary, compilationAssemblyResolvers);
                foreach (var referencePath in referencePaths)
                {
                    resolvedReferencePaths.Add(referencePath);
                }
            }
            return resolvedReferencePaths;
        }

        private IEnumerable<string> ResolveReferencePaths(CompilationLibrary compilationLibrary, ICompilationAssemblyResolver[] compilationAssemblyResolvers)
        {
            if (compilationLibrary.Assemblies.Any(a => a.EndsWith("_._")))
            {
                return Array.Empty<string>();
            }

            var referencePaths = compilationLibrary.ResolveReferencePaths(compilationAssemblyResolvers).ToArray();

            foreach (var referencePath in referencePaths)
            {
                _logger.Verbose($"{compilationLibrary.Name} => {referencePath}");
            }

            return referencePaths;
        }

        private ICompilationAssemblyResolver[] GetCompilationAssemblyResolvers(string[] nugetPackageFolders)
        {
            var resolvers = new List<ICompilationAssemblyResolver>
            {
                new AppBaseCompilationAssemblyResolver(),
                new ReferenceAssemblyPathResolver()
            };

            foreach (var nugetPackageFolder in nugetPackageFolders)
            {
                resolvers.Add(CreatePackageResolver(nugetPackageFolder));
            }
            return resolvers.ToArray();
        }

        private PackageCompilationAssemblyResolver CreatePackageResolver(string nugetGetPackageDirectory)
        {
            _logger.Verbose($"Creating {nameof(PackageCompilationAssemblyResolver)} for target path: {nugetGetPackageDirectory}");
            return new PackageCompilationAssemblyResolver(nugetGetPackageDirectory);
        }
    }
}