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

        public ScriptDependencyContextReader(LogFactory logFactory, ScriptFilesDependencyResolver scriptFilesDependencyResolver)
        {
            _logger = logFactory.CreateLogger<ScriptDependencyContextReader>();
            _nuGetLogger = new NuGetLogger(logFactory);
            _scriptFilesDependencyResolver = scriptFilesDependencyResolver;
        }

        public ScriptDependencyContextReader(LogFactory logFactory)
        : this(logFactory, new ScriptFilesDependencyResolver(logFactory))
        {
        }

        public ScriptDependencyContext ReadDependencyContext(string pathToAssetsFile)
        {
            var lockFile = LockFileUtilities.GetLockFile(pathToAssetsFile, _nuGetLogger);
            var libs = lockFile.Targets[1].Libraries;
            var target = lockFile.Targets[1];
            var packageFolders = lockFile.PackageFolders.Select(lfi => lfi.Path).ToArray();
            var userPackageFolder = packageFolders.First();
            var fallbackFolders = packageFolders.Skip(1);
            var packagePathResolver = new FallbackPackagePathResolver(userPackageFolder, fallbackFolders);

            List<ScriptDependency> scriptDependencies = new List<ScriptDependency>();
            foreach (var targetLibrary in libs)
            {
                var scriptDependency = CreateScriptDependency(targetLibrary.Name, targetLibrary.Version.ToString(), packageFolders, packagePathResolver, targetLibrary);
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

        private ScriptDependency CreateScriptDependency(string name, string version, string[] packageFolders, FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
        {
            var runtimeDependencyPaths = GetRuntimeDependencyPaths(packagePathResolver, targetLibrary);
            var compileTimeDependencyPaths = GetCompileTimeDependencyPaths(packagePathResolver, targetLibrary);
            var nativeAssetPaths = GetNativeAssetPaths(packagePathResolver, targetLibrary);
            var scriptPaths = GetScriptPaths(packagePathResolver, targetLibrary);

            return new ScriptDependency(name, version, runtimeDependencyPaths, nativeAssetPaths, compileTimeDependencyPaths.ToArray(), scriptPaths);
        }

        private string[] GetScriptPaths(FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
        {
            if (targetLibrary.ContentFiles.Count == 0)
            {
                return Array.Empty<string>();
            }

            var packageFolder = ResolvePackageFullPath(packagePathResolver, targetLibrary.Name, targetLibrary.Version.ToString());

            // Note that we can't use content files directly here since that only works for
            // script packages directly referenced by the script and not script packages having
            // dependencies to other script packages.

            var files = _scriptFilesDependencyResolver.GetScriptFileDependencies(packageFolder);
            return files;
        }

        private string[] GetNativeAssetPaths(FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
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
            var packageFolder = ResolvePackageFullPath(packagePathResolver, name, version);

            var fullPath = Path.Combine(packageFolder, referencePath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            string message = $@"The requested dependency ({referencePath}) was not found in the global Nuget cache(s).
. Try executing/publishing the script again with the '--no-cache' option";
            throw new InvalidOperationException(message);
        }


        private static string ResolvePackageFullPath(FallbackPackagePathResolver packagePathResolver, string name, string version)
        {
            var packageFolder = packagePathResolver.GetPackageDirectory(name, version);
            if (packageFolder != null)
            {
                return packageFolder;
            }

            string message = $@"The requested package ({name},{version}) was not found in the global Nuget cache(s).
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
}