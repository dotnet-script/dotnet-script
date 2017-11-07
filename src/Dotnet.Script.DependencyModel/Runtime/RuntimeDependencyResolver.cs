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
        private readonly Logger _logger;

        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        private RuntimeDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyInfoProvider scriptDependencyInfoProvider, LogFactory logFactory)
        {            
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyInfoProvider = scriptDependencyInfoProvider;
            _logger = logFactory.CreateLogger<RuntimeDependencyResolver>();
        }

        public RuntimeDependencyResolver(LogFactory logFactory) 
            : this
            (                  
                  new ScriptProjectProvider(logFactory), 
                  new ScriptDependencyInfoProvider(CreateRestorers(logFactory), logFactory),
                  logFactory
            )
        {
        }

        private static IRestorer[] CreateRestorers(LogFactory logFactory)
        {
            var commandRunner = new CommandRunner(logFactory);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logFactory)};
        }

        public IEnumerable<RuntimeDependency> GetDependenciesFromCode(string targetDirectory, string code)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProjectForRepl(code, targetDirectory, "netcoreapp2.0");
            return GetDependenciesInternal(pathToProjectFile);
        }


        public IEnumerable<RuntimeDependency> GetDependencies(string targetDirectory)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, "netcoreapp1.1", true);
            return GetDependenciesInternal(pathToProjectFile);
        }

        private IEnumerable<RuntimeDependency> GetDependenciesInternal(string pathToProjectFile)
        {
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
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(
                nlg => RuntimeHelper.AppliesToCurrentRuntime(nlg.Runtime)))
            {

                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = GetFullPath(Path.Combine(runtimeLibrary.Path, assetPath), nugetPackageFolders);
                    _logger.Debug($"Loading native library from {fullPath}");
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
            var runtimeAssemblyGroup =
                runtimeLibrary.RuntimeAssemblyGroups.FirstOrDefault(rag =>
                    rag.Runtime == RuntimeHelper.GetPlatformIdentifier());

            if (runtimeAssemblyGroup == null)
            {
                runtimeAssemblyGroup =
                    runtimeLibrary.RuntimeAssemblyGroups.FirstOrDefault(rag => string.IsNullOrWhiteSpace(rag.Runtime));
            }
            if (runtimeAssemblyGroup == null)
            {
                return;
            }
            //var runtimeAssemblyGroups = runtimeLibrary.RuntimeAssemblyGroups.Where(rag =>
            //    string.IsNullOrWhiteSpace(rag.Runtime) || rag.Runtime == RuntimeHelper.GetPlatformIdentifier()).ToArray();
            //foreach (var runtimeAssemblyGroup in runtimeAssemblyGroups)
            //{
                foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    if (!path.EndsWith("_._"))
                    {
                        var fullPath = GetFullPath(path, nugetPackageFolders);
                        
                        _logger.Debug($"Resolved runtime library {runtimeLibrary.Name} located at {fullPath}");
                        resolvedDependencies.Add(new RuntimeDependency(AssemblyName.GetAssemblyName(fullPath), fullPath));
                    }
                }
            //}
        }

        private static string GetFullPath(string relativePath, IEnumerable<string> nugetPackageFolders)
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