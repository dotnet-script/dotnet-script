using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
            var pathToProjectFile = _scriptProjectProvider.CreateProject(targetDirectory, "netcoreapp2.0", true);
            return GetDependenciesInternal(pathToProjectFile);
        }

        private IEnumerable<RuntimeDependency> GetDependenciesInternal(string pathToProjectFile)
        {
            var dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectFile);

            var dependencyContext = dependencyInfo.DependencyContext;
            List<string> nuGetPackageFolders = dependencyInfo.NugetPackageFolders.ToList();
            nuGetPackageFolders.Add(RuntimeHelper.GetPathToNuGetStoreFolder());

            var runtimeDepedencies = new List<RuntimeDependency>();

            var runtimeLibraries = dependencyContext.RuntimeLibraries;

            
            foreach (var runtimeLibrary in runtimeLibraries)
            {
                var runtimeDependency = new RuntimeDependency(runtimeLibrary.Name, runtimeLibrary.Version,
                    ProcessRuntimeAssemblies(runtimeLibrary, nuGetPackageFolders.ToArray()),
                    ProcessNativeLibraries(runtimeLibrary, nuGetPackageFolders.ToArray()),
                    ProcessScriptFiles(runtimeLibrary, nuGetPackageFolders.ToArray()));

                runtimeDepedencies.Add(runtimeDependency);
            }

            return runtimeDepedencies;
        }

        private string[] ProcessScriptFiles(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            var result = new List<string>();
            var packagePath = GetPackagePath(runtimeLibrary, nugetPackageFolders);            
            var scriptFiles = Directory.GetFiles(packagePath, "*.csx", SearchOption.AllDirectories);
            
            
            if (scriptFiles.Length > 0)
            {
                var netstandard20ScriptFiles = scriptFiles.Where(IsNetStandard20).ToArray();
                var rootPath = GetRootPath(netstandard20ScriptFiles[0]);
                var mainCsx = Directory.GetFiles(rootPath, "*.csx")
                    .FirstOrDefault(f => Path.GetFileName(f).ToLower() == "main.csx");
                if (mainCsx != null)
                {
                    //Note: Also allow single root file.
                    result.Add(mainCsx);
                }
                else
                {
                    result.AddRange(netstandard20ScriptFiles);
                }                
            }
            return result.ToArray();
        }

        private string GetRootPath(string pathToScriptFile)
        {
            string pattern = @"(^.*contentFiles[\/,\\]cs[\/,\\]netstandard2.0[\/,\\]).*$";
            var match = Regex.Match(pathToScriptFile, pattern);
            return match.Groups[1].Value;
        }
      

        private bool IsNetStandard20(string pathToScriptFile)
        {
            // We keep it simple for now accepting only "netstandard" scripts.
            var pattern = @"^.*contentFiles[\/,\\]cs[\/,\\]netstandard2.0.*$";
            return Regex.IsMatch(pathToScriptFile, pattern, RegexOptions.IgnoreCase);
        }


        private string[] ProcessNativeLibraries(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            List<string> result = new List<string>();
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(
                nlg => RuntimeHelper.AppliesToCurrentRuntime(nlg.Runtime)))
            {

                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = GetFullPath(Path.Combine(runtimeLibrary.Path, assetPath), nugetPackageFolders);
                    _logger.Debug($"Loading native library from {fullPath}");
                    result.Add(fullPath);
                }
            }
            return result.ToArray();
        }
        private RuntimeAssembly[] ProcessRuntimeAssemblies(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            var result = new List<RuntimeAssembly>();

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
                return Array.Empty<RuntimeAssembly>();
            }            
            foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
            {
                var path = Path.Combine(runtimeLibrary.Path, assetPath);
                if (!path.EndsWith("_._"))
                {
                    var fullPath = GetFullPath(path, nugetPackageFolders);
                        
                    _logger.Debug($"Resolved runtime library {runtimeLibrary.Name} located at {fullPath}");
                    result.Add(new RuntimeAssembly(AssemblyName.GetAssemblyName(fullPath),fullPath));                    
                }
            }
            return result.ToArray();
        }


        private string GetPackagePath(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            foreach (var nugetPackageFolder in nugetPackageFolders)
            {
                var packagePath = Path.Combine(nugetPackageFolder, runtimeLibrary.Path);
                if (Directory.Exists(packagePath))
                {
                    return packagePath;
                }
            }

            throw new InvalidOperationException("Not found");
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