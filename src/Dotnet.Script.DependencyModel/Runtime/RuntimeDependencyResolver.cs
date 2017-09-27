using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Runtime
{

    public class RuntimeDependencyResolver
    {
        private readonly DependencyPathResolver _dependencyPathResolver;
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly DependencyContextProvider _dependencyContextProvider;
        private readonly Action<bool, string> _logger;

        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        private RuntimeDependencyResolver(DependencyPathResolver dependencyPathResolver, ScriptProjectProvider scriptProjectProvider, DependencyContextProvider dependencyContextProvider,  Action<bool, string> logger)
        {
            _dependencyPathResolver = dependencyPathResolver;
            _scriptProjectProvider = scriptProjectProvider;
            _dependencyContextProvider = dependencyContextProvider;
            _logger = logger;
        }

        public RuntimeDependencyResolver(Action<bool, string> logger) 
            : this
            (
                  new DependencyPathResolver(logger),
                  new ScriptProjectProvider(logger), 
                  new DependencyContextProvider(CreateRestorers(logger),logger), 
                  logger
            )
        {            
        }

        private static IRestorer[] CreateRestorers(Action<bool, string> logger)
        {
            var commandRunner = new CommandRunner(logger);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logger)};
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string targetDirectory)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, "netcoreapp.20", true);
            var dependencyContext = _dependencyContextProvider.GetDependencyContext(pathToProjectFile);

            var runtimeDepedencies = new HashSet<RuntimeDependency>();

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
            HashSet<RuntimeDependency> resolvedDependencies)
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
                        resolvedDependencies.Add(new RuntimeDependency(runtimeLibrary.Name, fullPath));
                    }
                }
            }
        }
    }
}