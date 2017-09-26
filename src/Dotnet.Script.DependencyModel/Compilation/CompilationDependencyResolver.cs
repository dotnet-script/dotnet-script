using System;
using System.Collections.Generic;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver 
    {
        private readonly Action<bool, string> _logger;

        public CompilationDependencyResolver(Action<bool, string> logger)
        {
            _logger = logger;
        }

        public static CompilationDependencyResolver Create(Action<bool, string> logger,
            bool enableScriptNuGetReferences)
        {
            return null;
        }

        public IEnumerable<string> GetDependencies(DependencyContext dependencyContext)
        {
            var resolvedReferencePaths = new HashSet<string>();
            
            var compileLibraries = dependencyContext.CompileLibraries;

            foreach (var compilationLibrary in compileLibraries)
            {                
                _logger.Verbose($"Resolving compilation reference paths for {compilationLibrary.Name}");
                var referencePaths = TryResolveReferencePaths(compilationLibrary);
                foreach (var referencePath in referencePaths)
                {
                    resolvedReferencePaths.Add(referencePath);
                }
            }
            return resolvedReferencePaths;
        }

        private IEnumerable<string> TryResolveReferencePaths(CompilationLibrary compilationLibrary)
        {
            try
            {
                return compilationLibrary.ResolveReferencePaths(GetCompilationAssemblyResolvers());
            }
            catch (Exception e)
            {
                _logger.Log($"Unable to resolve reference paths for {compilationLibrary.Name} ({compilationLibrary.Path}) Exception details : {e}");
            }
            return Array.Empty<string>();
        }

        private static ICompilationAssemblyResolver[] GetCompilationAssemblyResolvers()
        {
            List<ICompilationAssemblyResolver> resolvers = new List<ICompilationAssemblyResolver>();
            resolvers.Add(new AppBaseCompilationAssemblyResolver());
            resolvers.Add(new ReferenceAssemblyPathResolver());
            resolvers.Add(new PackageCompilationAssemblyResolver(RuntimeHelper.GetPathToNuGetFallbackFolder()));
            resolvers.Add(new PackageCompilationAssemblyResolver(RuntimeHelper.GetPathToGlobalPackagesFolder()));
            return resolvers.ToArray();
        }

    }
}