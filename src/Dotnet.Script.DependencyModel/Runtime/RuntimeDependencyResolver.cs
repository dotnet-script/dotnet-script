using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptDependencyInfoProvider _scriptDependencyInfoProvider;
        private readonly Action<bool, string> _logger;
        

        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        private RuntimeDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyInfoProvider scriptDependencyInfoProvider,  Action<bool, string> logger)
        {            
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyInfoProvider = scriptDependencyInfoProvider;
            _logger = logger;
        }

        public RuntimeDependencyResolver(Action<bool, string> logger) 
            : this
            (                  
                  new ScriptProjectProvider(logger), 
                  new ScriptDependencyInfoProvider(CreateRestorers(logger),logger), 
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
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, "netcoreapp2.0", true);
            var dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectFile);
            
            var dependencyContext = dependencyInfo.DependencyContext;
            List<string> nuGetPackageFolders = dependencyInfo.NugetPackageFolders.ToList();
            nuGetPackageFolders.Add(RuntimeHelper.GetPathToNuGetStoreFolder());

            var runtimeDepedencies = new HashSet<RuntimeDependency>();

            var runtimeLibraries = dependencyContext.RuntimeLibraries;

            foreach (var runtimeLibrary in runtimeLibraries)
            {                
                ProcessNativeLibraries(runtimeLibrary, nuGetPackageFolders.ToArray());
                ProcessRuntimeAssemblies(runtimeLibrary, runtimeDepedencies, nuGetPackageFolders.ToArray());
            }

            return runtimeDepedencies;
        }

        private void ProcessNativeLibraries(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            if (runtimeLibrary.Name.ToLower().Contains("e_sqlite"))
            {
                
            }

            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(
                nlg => RuntimeHelper.AppliesToCurrentRuntime(nlg.Runtime)))
            {                

                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = GetFullPath(Path.Combine(runtimeLibrary.Path, assetPath), nugetPackageFolders);
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
            HashSet<RuntimeDependency> resolvedDependencies, string[] nugetPackageFolders)
        {            
            foreach (var runtimeAssemblyGroup in runtimeLibrary.RuntimeAssemblyGroups.Where(rag => RuntimeHelper.AppliesToCurrentRuntime(rag.Runtime)))
            {
                foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    if (!path.EndsWith("_._"))
                    {
                        var fullPath = GetFullPath(path, nugetPackageFolders);
                        
                        _logger.Verbose($"Resolved runtime library {runtimeLibrary.Name} located at {fullPath}");
                        resolvedDependencies.Add(new RuntimeDependency(AssemblyName.GetAssemblyName(fullPath), fullPath));
                    }
                }
            }
        }

        public string GetFullPath(string relativePath, IEnumerable<string> nugetPackageFolders)
        {
            foreach (var possibleLocation in nugetPackageFolders)
            {
                var fullPath = Path.Combine(possibleLocation, relativePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            throw new InvalidOperationException("Not found");
        }
    }
}