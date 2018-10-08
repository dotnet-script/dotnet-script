using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.ScriptPackage;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver 
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptDependencyInfoProvider _scriptDependencyInfoProvider;
        private readonly ScriptFilesDependencyResolver _scriptFilesDependencyResolver;
        private readonly Logger _logger;

        private CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyInfoProvider scriptDependencyInfoProvider, ScriptFilesDependencyResolver scriptFilesDependencyResolver,  LogFactory logFactory)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyInfoProvider = scriptDependencyInfoProvider;
            _scriptFilesDependencyResolver = scriptFilesDependencyResolver;
            _logger = logFactory.CreateLogger<CompilationDependencyResolver>();
        }

        public CompilationDependencyResolver(LogFactory logFactory) 
            : this
            (
                new ScriptProjectProvider(logFactory), 
                new ScriptDependencyInfoProvider(CreateRestorers(logFactory), logFactory),
                new ScriptFilesDependencyResolver(logFactory), 
                logFactory
            )
        { }
        
        private static IRestorer[] CreateRestorers(LogFactory logFactory)
        {
            var commandRunner = new CommandRunner(logFactory);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logFactory), new NuGetRestorer(commandRunner, logFactory) };
        }
        
        public IEnumerable<CompilationDependency> GetDependencies(string targetDirectory, bool enableScriptNugetReferences, string defaultTargetFramework = "net46", string temporaryDirectory = null)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, defaultTargetFramework,
                enableScriptNugetReferences, temporaryDirectory);

            if (pathToProjectFile == null)
            {
                return Array.Empty<CompilationDependency>();
            }

            var dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectFile, Array.Empty<string>());

            var dependencyContext = dependencyInfo.DependencyContext;

            var compilationAssemblyResolvers = GetCompilationAssemblyResolvers(dependencyInfo.NugetPackageFolders);
           
            
            List<CompilationDependency> result = new List<CompilationDependency>();            
            var compileLibraries = dependencyContext.CompileLibraries;

            foreach (var compilationLibrary in compileLibraries)
            {
                var resolvedReferencePaths = new HashSet<string>();
                _logger.Trace($"Resolving compilation reference paths for {compilationLibrary.Name}");
                var referencePaths = ResolveReferencePaths(compilationLibrary, compilationAssemblyResolvers);
                var scripts =
                    _scriptFilesDependencyResolver.GetScriptFileDependencies(compilationLibrary.Path, dependencyInfo.NugetPackageFolders);
                foreach (var referencePath in referencePaths)
                {
                    resolvedReferencePaths.Add(referencePath);
                }
                var compilationDependency = new CompilationDependency(
                    compilationLibrary.Name,
                    compilationLibrary.Version, 
                    resolvedReferencePaths.ToList(), 
                    scripts);

                result.Add(compilationDependency);
            }
            return result;
        }

        private IEnumerable<string> ResolveReferencePaths(CompilationLibrary compilationLibrary, ICompilationAssemblyResolver[] compilationAssemblyResolvers)
        {
            if (compilationLibrary.Assemblies.Any(a => a.EndsWith("_._")))
            {
                return Array.Empty<string>();
            }            
            var referencePaths = compilationLibrary.ResolveReferencePaths(compilationAssemblyResolvers).Select(p => Path.GetFullPath(p)).ToArray();

            foreach (var referencePath in referencePaths)
            {
                _logger.Trace($"{compilationLibrary.Name} => {referencePath}");
            }

            return referencePaths;
        }

        private ICompilationAssemblyResolver[] GetCompilationAssemblyResolvers(string[] nugetPackageFolders)
        {
            var resolvers = new List<ICompilationAssemblyResolver>();
            

            foreach (var nugetPackageFolder in nugetPackageFolders)
            {
                resolvers.Add(CreatePackageResolver(nugetPackageFolder));
            }
            return resolvers.ToArray();
        }

        private PackageCompilationAssemblyResolver CreatePackageResolver(string nugetGetPackageDirectory)
        {
            _logger.Debug($"Creating {nameof(PackageCompilationAssemblyResolver)} for target path: {nugetGetPackageDirectory}");
            return new PackageCompilationAssemblyResolver(nugetGetPackageDirectory);
        }
    }

    public class CompilationDependency
    {
        public CompilationDependency(string name, string version, IReadOnlyList<string> assemblyPaths, IReadOnlyList<string> scripts)
        {
            Name = name;
            Version = version;
            AssemblyPaths = assemblyPaths;
            Scripts = scripts;
        }

        public string Name { get; }

        public string Version { get; }

        public IReadOnlyList<string> AssemblyPaths { get; }
        
        public IReadOnlyList<string> Scripts { get; }

        public override string ToString()
        {
            return $"Name: {Name} , Version: {Version}";
        }
    }
}