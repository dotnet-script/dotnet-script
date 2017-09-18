using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel
{
    public class RuntimeDependencyResolver : IDependencyResolver
    {
        private readonly IDependencyPathResolver _dependencyPathResolver;
        private readonly Action<bool, string> _logger;

        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        public RuntimeDependencyResolver(IDependencyPathResolver dependencyPathResolver, Action<bool, string> logger)
        {
            _dependencyPathResolver = dependencyPathResolver;
            _logger = logger;
        }

        public IEnumerable<ResolvedDependency> GetDependencies(DependencyContext dependencyContext)
        {
            var runtimeDepedencies = new HashSet<ResolvedDependency>();

            var runtimeLibraries = dependencyContext.RuntimeLibraries;

            foreach (var runtimeLibrary in runtimeLibraries)
            {
                ProcessNativeLibraries(runtimeLibrary);
                ProcessRuntimeAssemblies(runtimeLibrary, runtimeDepedencies);
            }

            return runtimeDepedencies;
        }

        private void ProcessNativeLibraries(RuntimeLibrary runtimeLibrary)
        {
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(
                nlg => RuntimeHelper.AppliesToCurrentRuntime(nlg.Runtime)))
            {
                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = _dependencyPathResolver.GetFullPath(Path.Combine(runtimeLibrary.Path, assetPath));
                    _logger.Verbose($"Loading native library from {fullPath}");
                    if (RuntimeHelper.IsWindows())
                    {
                        LoadLibrary(fullPath);
                    }
                    else
                    {
                        // Maybe something like this?
                        // https://stackoverflow.com/questions/13461989/p-invoke-to-dynamically-loaded-library-on-mono
                    }
                }
            }
        }
        private void ProcessRuntimeAssemblies(RuntimeLibrary runtimeLibrary,
            HashSet<ResolvedDependency> resolvedDependencies)
        {            
            foreach (var runtimeAssemblyGroup in runtimeLibrary.RuntimeAssemblyGroups.Where(rag => RuntimeHelper.AppliesToCurrentRuntime(rag.Runtime)))
            {
                foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    if (!path.EndsWith("_._"))
                    {
                        var fullPath = _dependencyPathResolver.GetFullPath(path);
                        
                        _logger.Verbose($"Resolved runtime library {runtimeLibrary.Name} located at {fullPath}");
                        resolvedDependencies.Add(new ResolvedDependency(runtimeLibrary.Name, fullPath));
                    }
                }
            }
        }
    }
}