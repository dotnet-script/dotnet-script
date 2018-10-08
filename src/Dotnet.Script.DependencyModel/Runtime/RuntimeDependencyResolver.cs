using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.ScriptPackage;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Runtime
{
    public class RuntimeDependencyResolver
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptDependencyInfoProvider _scriptDependencyInfoProvider;
        private readonly ScriptFilesDependencyResolver _scriptFilesDependencyResolver;
        private readonly Logger _logger;
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly Regex _runtimeMatcher;

        private RuntimeDependencyResolver(ScriptProjectProvider scriptProjectProvider, ScriptDependencyInfoProvider scriptDependencyInfoProvider, ScriptFilesDependencyResolver scriptFilesDependencyResolver, LogFactory logFactory, ScriptEnvironment scriptEnvironment)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _scriptDependencyInfoProvider = scriptDependencyInfoProvider;
            _scriptFilesDependencyResolver = scriptFilesDependencyResolver;
            _logger = logFactory.CreateLogger<RuntimeDependencyResolver>();
            _scriptEnvironment = scriptEnvironment;
            _runtimeMatcher = new Regex($"{_scriptEnvironment.PlatformIdentifier}.*-{_scriptEnvironment.ProccessorArchitecture}");
        }

        public RuntimeDependencyResolver(LogFactory logFactory)
            : this
            (
                  new ScriptProjectProvider(logFactory),
                  new ScriptDependencyInfoProvider(CreateRestorers(logFactory), logFactory),
                  new ScriptFilesDependencyResolver(logFactory),
                  logFactory,
                  ScriptEnvironment.Default
            )
        {
        }

        private static IRestorer[] CreateRestorers(LogFactory logFactory)
        {
            var commandRunner = new CommandRunner(logFactory);
            return new IRestorer[] { new DotnetRestorer(commandRunner, logFactory) };
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string targetDirectory, ScriptMode scriptMode, string[] packagesSources, string code = null, string temporaryDirectory = null)
        {
            var pathToProjectFile = scriptMode == ScriptMode.Script
                ? _scriptProjectProvider.CreateProject(targetDirectory, _scriptEnvironment.TargetFramework, true, temporaryDirectory)
                : _scriptProjectProvider.CreateProjectForRepl(code, Path.Combine(targetDirectory, scriptMode.ToString()), ScriptEnvironment.Default.TargetFramework, temporaryDirectory);

            return GetDependenciesInternal(pathToProjectFile, packagesSources);
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string scriptFile, string[] packagesSources, string temporaryDirectory)
        {
            var pathToProjectFile = _scriptProjectProvider.CreateProjectForScriptFile(scriptFile, temporaryDirectory);
            return GetDependenciesInternal(pathToProjectFile, packagesSources);
        }

        public IEnumerable<RuntimeDependency> GetDependencies(string dllPath)
        {
            return GetDependenciesInternal(dllPath, restorePackages: false);
        }

        private IEnumerable<RuntimeDependency> GetDependenciesInternal(string pathToProjectOrDll, string[] packageSources = null, bool restorePackages = true)
        {
            packageSources = packageSources ?? new string[0];
            ScriptDependencyInfo dependencyInfo;
            if (restorePackages)
                dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectOrDll, packageSources);
            else
                dependencyInfo = _scriptDependencyInfoProvider.GetDependencyInfo(pathToProjectOrDll);

            var dependencyContext = dependencyInfo.DependencyContext;
            List<string> nuGetPackageFolders = dependencyInfo.NugetPackageFolders.ToList();
            nuGetPackageFolders.Add(_scriptEnvironment.NuGetStoreFolder);

            var runtimeDependencies = new List<RuntimeDependency>();

            var runtimeLibraries = dependencyContext.RuntimeLibraries;

            foreach (var runtimeLibrary in runtimeLibraries)
            {
                var runtimeDependency = new RuntimeDependency(runtimeLibrary.Name, runtimeLibrary.Version,
                    ProcessRuntimeAssemblies(runtimeLibrary, nuGetPackageFolders.ToArray()),
                    ProcessNativeLibraries(runtimeLibrary, nuGetPackageFolders.ToArray()),
                    ProcessScriptFiles(runtimeLibrary, nuGetPackageFolders.ToArray()));

                runtimeDependencies.Add(runtimeDependency);
            }

            return runtimeDependencies;
        }

        private string[] ProcessScriptFiles(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            return _scriptFilesDependencyResolver.GetScriptFileDependencies(runtimeLibrary.Path, nugetPackageFolders);
        }

        private string[] ProcessNativeLibraries(RuntimeLibrary runtimeLibrary, string[] nugetPackageFolders)
        {
            List<string> result = new List<string>();
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(
                nlg => AppliesToCurrentRuntime(nlg.Runtime)))
            {

                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = GetFullPath(Path.Combine(runtimeLibrary.Path, assetPath), nugetPackageFolders);
                    _logger.Trace($"Loading native library from {fullPath}");
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
                    rag.Runtime == _scriptEnvironment.RuntimeIdentifier);

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

                    _logger.Trace($"Resolved runtime library {runtimeLibrary.Name} located at {fullPath}");
                    result.Add(new RuntimeAssembly(AssemblyName.GetAssemblyName(fullPath), fullPath));
                }
            }
            return result.ToArray();
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

        private bool AppliesToCurrentRuntime(string runtime)
        {
            return string.IsNullOrWhiteSpace(runtime) || _runtimeMatcher.IsMatch(runtime);
        }
    }
}