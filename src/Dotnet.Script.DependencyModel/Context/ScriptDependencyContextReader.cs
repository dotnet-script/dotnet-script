using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ScriptPackage;
using Microsoft.DotNet.PlatformAbstractions;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;

namespace Dotnet.Script.DependencyModel.Context
{
    public class ScriptDependencyContextReader
    {
        private readonly Logger _logger;
        private readonly ILogger _nuGetLogger;
        private readonly ScriptFilesDependencyResolver _scriptFilesDependencyResolver;
        private const string RuntimeJsonFileName = "runtime.json";

        public ScriptDependencyContextReader(LogFactory logFactory, ScriptFilesDependencyResolver scriptFilesDependencyResolver)
        {
            _logger = logFactory.CreateLogger<ScriptDependencyContextReader>();
            _nuGetLogger = new NuGetLogger(logFactory);
            this._scriptFilesDependencyResolver = scriptFilesDependencyResolver;
        }

        public ScriptDependencyContextReader(LogFactory logFactory)
        : this(logFactory, new ScriptFilesDependencyResolver(logFactory))
        {
        }

        public ScriptDependencyContext ReadDependencyContext(string pathToAssetsFile)
        {
            var lockFile = LockFileUtilities.GetLockFile(pathToAssetsFile, _nuGetLogger);
                var runtimeGraph = Collect(lockFile);
                var runtimes = runtimeGraph.ExpandRuntime(RuntimeEnvironment.GetRuntimeIdentifier()).ToArray();
                var libs = lockFile.Targets[1].Libraries;
                var target = lockFile.Targets[1];
                var packageFolders = lockFile.PackageFolders.Select(lfi => lfi.Path).ToArray();
                var userPackageFolder = packageFolders.First();
                var fallbackFolders = packageFolders.Skip(1);
                var packagePathResolver = new FallbackPackagePathResolver(userPackageFolder, fallbackFolders);

                List<ScriptDependency> scriptDependencies = new List<ScriptDependency>();
                foreach (var targetLibrary in libs)
                {
                    var scriptDependency = CreateScriptDependency(targetLibrary.Name, targetLibrary.Version.ToString(), packageFolders, packagePathResolver, runtimes, targetLibrary);
                    if (scriptDependency.CompileTimeDependencyPaths.Any() ||
                        scriptDependency.NativeAssetPaths.Any() ||
                        scriptDependency.RuntimeDependencyPaths.Any() ||
                        scriptDependency.ScriptPaths.Any())
                    {
                        scriptDependencies.Add(scriptDependency);
                    }
                }

                return new ScriptDependencyContext(scriptDependencies.ToArray());
        }

        private static RuntimeGraph Collect(LockFile lockFile)
            {
                string userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
                var fallBackFolders = lockFile.PackageFolders.Skip(1).Select(f => f.Path);
                var packageResolver = new FallbackPackagePathResolver(userPackageFolder, fallBackFolders);

                var graph = RuntimeGraph.Empty;
                foreach (var library in lockFile.Libraries)
                {
                    if (string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
                    {
                        var runtimeJson = library.Files.FirstOrDefault(f => f == RuntimeJsonFileName);
                        if (runtimeJson != null)
                        {
                            var libraryPath = packageResolver.GetPackageDirectory(library.Name, library.Version);
                            var runtimeJsonFullName = Path.Combine(libraryPath, runtimeJson);
                            graph = RuntimeGraph.Merge(graph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonFullName));
                        }
                    }
                }
                return graph;
            }


        private ScriptDependency CreateScriptDependency(string name, string version, string[] packageFolders, FallbackPackagePathResolver packagePathResolver, string[] runtimes, LockFileTargetLibrary targetLibrary)
        {
            var runtimeDependencyPaths = GetRuntimeDependencyPaths(packagePathResolver, targetLibrary);
            var compileTimeDependencyPaths = GetCompileTimeDependencyPaths(packagePathResolver, targetLibrary);
            var runtimeSpecificDependencyPaths = GetRuntimeSpecificDependencyPaths(packagePathResolver, runtimes, targetLibrary);
            var nativeAssetPaths = GetNativeAssetPaths(packagePathResolver, runtimes, targetLibrary);
            var scriptPaths = GetScriptPaths(packageFolders, targetLibrary);
            var allRuntimeDependencyPaths = runtimeDependencyPaths.Concat(runtimeSpecificDependencyPaths).ToArray();

            return new ScriptDependency(name, version, allRuntimeDependencyPaths, nativeAssetPaths, compileTimeDependencyPaths.ToArray(), scriptPaths);
        }

        private string[] GetScriptPaths(string[] packageFolders, LockFileTargetLibrary targetLibrary)
        {
            if (targetLibrary.ContentFiles.Count == 0)
            {
                return Array.Empty<string>();
            }

            // Note that we can't use content files directly here since that only works for
            // script packages directly referenced by the script and not script packages having
            // dependencies to other script packages.

            var files = _scriptFilesDependencyResolver.GetScriptFileDependencies(Path.Combine(targetLibrary.Name, targetLibrary.Version.ToString()), packageFolders);
            return files;
        }

        private string[] GetRuntimeSpecificDependencyPaths(FallbackPackagePathResolver packagePathResolver, string[] runtimes, LockFileTargetLibrary targetLibrary)
        {
            List<string> runtimeSpecificDependencyPaths = new List<string>();
            foreach (var runtimeTarget in targetLibrary.RuntimeTargets.Where(rt => rt.AssetType.Equals("runtime")))
            {
                if (runtimes.Contains(runtimeTarget.Runtime, StringComparer.OrdinalIgnoreCase) && !runtimeTarget.Path.EndsWith("_._"))
                {
                    var fullPath = ResolveFullPath(packagePathResolver, targetLibrary.Name, targetLibrary.Version.ToString(), runtimeTarget.Path);
                    runtimeSpecificDependencyPaths.Add(fullPath);
                }
            }

            return runtimeSpecificDependencyPaths.ToArray();;
        }

        private string[] GetNativeAssetPaths(FallbackPackagePathResolver packagePathResolver, string[] runtimes, LockFileTargetLibrary targetLibrary)
        {
            List<string> nativeAssetPaths = new List<string>();
            foreach (var runtimeTarget in targetLibrary.NativeLibraries)
            {
                    var fullPath = ResolveFullPath(packagePathResolver, targetLibrary.Name, targetLibrary.Version.ToString(), runtimeTarget.Path);
                    nativeAssetPaths.Add(fullPath);
            }

            return nativeAssetPaths.ToArray();
        }

        private static string[] GetRuntimeDependencyPaths(FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
        {
            List<string> runtimeDependencyPaths = new List<string>();

            foreach (var lockFileItem in targetLibrary.RuntimeAssemblies.Where(lfi => !lfi.Path.EndsWith("_._")))
            {
                var fullPath = ResolveFullPath(packagePathResolver, targetLibrary.Name, targetLibrary.Version.ToString(), lockFileItem.Path);
                runtimeDependencyPaths.Add(fullPath);
            }

            return runtimeDependencyPaths.ToArray();
        }


        private static string[] GetCompileTimeDependencyPaths(FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
        {
            var compileTimeDependencyPaths = new List<string>();

            foreach (var lockFileItem in targetLibrary.CompileTimeAssemblies.Where(cta => !cta.Path.EndsWith("_._")))
            {
                var fullPath = ResolveFullPath(packagePathResolver, targetLibrary.Name, targetLibrary.Version.ToString(), lockFileItem.Path);
                compileTimeDependencyPaths.Add(fullPath);
            }
            return compileTimeDependencyPaths.ToArray();
        }

        private static string ResolveFullPath(FallbackPackagePathResolver packagePathResolver, string name, string version, string referencePath)
        {
            var packageFolder = packagePathResolver.GetPackageDirectory(name, version);
            if (packageFolder != null)
            {
                var fullPath = Path.Combine(packageFolder, referencePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            string message = $@"The requested dependency ({referencePath}) was not found in the global Nuget cache(s).
. Try executing/publishing the script again with the '--no-cache' option";
            throw new InvalidOperationException(message);
        }


        private class NuGetLogger : LoggerBase
        {
            private readonly Logger _logger;

            public NuGetLogger(LogFactory logFactory)
            {
                _logger = logFactory.CreateLogger<NuGetLogger>();
            }

            public override void Log(ILogMessage message)
            {
                if (message.Level == NuGet.Common.LogLevel.Debug)
                {
                    _logger.Debug(message.Message);
                }

                else if (message.Level == NuGet.Common.LogLevel.Verbose)
                {
                    _logger.Trace(message.Message);
                }

                else if (message.Level == NuGet.Common.LogLevel.Information)
                {
                    _logger.Info(message.Message);
                }

                else if (message.Level == NuGet.Common.LogLevel.Error)
                {
                    _logger.Error(message.Message);
                }

                else if (message.Level == NuGet.Common.LogLevel.Minimal)
                {
                    _logger.Info(message.Message);
                }
            }

            public override Task LogAsync(ILogMessage message)
            {
                Log(message);
                return Task.CompletedTask;
            }
        }
    }



    public class ScriptDependencyContext
    {
        public ScriptDependencyContext(ScriptDependency[] dependencies)
        {
            Dependencies = dependencies;
        }

        public ScriptDependency[] Dependencies { get; }
    }


    public class ScriptDependency
    {
        public ScriptDependency(string name, string version, string[] runtimeDependencyPaths, string[] nativeAssetPaths, string[] compileTimeDependencyPaths, string[] scriptPaths)
        {
            Name = name;
            Version = version;
            RuntimeDependencyPaths = runtimeDependencyPaths;
            NativeAssetPaths = nativeAssetPaths;
            CompileTimeDependencyPaths = compileTimeDependencyPaths;
            ScriptPaths = scriptPaths;
        }

        public string Name { get; }
        public string Version { get; }
        public string[] RuntimeDependencyPaths { get; }
        public string[] NativeAssetPaths { get; }
        public string[] CompileTimeDependencyPaths { get; }
        public string[] ScriptPaths { get; }

        public override string ToString()
        {
            return $"{Name}, {Version}";
        }
    }
}