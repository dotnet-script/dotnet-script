using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ScriptPackage;
using Microsoft.DotNet.PlatformAbstractions;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;
using NuGet.Versioning;

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
            var lockFile = GetLockFile(pathToAssetsFile);
            // Since we execute "dotnet restore -r [rid]" we get two targets in the lock file.
            // The second target is the one containing the runtime deps for the given RID.
            var target = GetLockFileTarget(lockFile);
            var targetLibraries = target.Libraries;
            var packageFolders = lockFile.PackageFolders.Select(lfi => lfi.Path).ToArray();
            var userPackageFolder = packageFolders.First();
            var fallbackFolders = packageFolders.Skip(1);
            var packagePathResolver = new FallbackPackagePathResolver(userPackageFolder, fallbackFolders);

            List<ScriptDependency> scriptDependencies = new List<ScriptDependency>();
            foreach (var targetLibrary in targetLibraries)
            {
                var scriptDependency = CreateScriptDependency(targetLibrary.Name, targetLibrary.Version.ToString(), packagePathResolver, targetLibrary);
                if (
                    scriptDependency.NativeAssetPaths.Any() ||
                    scriptDependency.RuntimeDependencyPaths.Any() ||
                    scriptDependency.CompileTimeDependencyPaths.Any() ||
                    scriptDependency.ScriptPaths.Any())
                {
                    scriptDependencies.Add(scriptDependency);
                }
            }

            if (ScriptEnvironment.Default.NetCoreVersion.Major >= 3)
            {
                var netcoreAppRuntimeAssemblyLocation = Path.GetDirectoryName(typeof(object).Assembly.Location);
                var netcoreAppRuntimeAssemblies = Directory.GetFiles(netcoreAppRuntimeAssemblyLocation, "*.dll").Where(IsAssembly).ToArray();
                var netCoreAppDependency = new ScriptDependency("Microsoft.NETCore.App", ScriptEnvironment.Default.NetCoreVersion.Version, netcoreAppRuntimeAssemblies, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
                scriptDependencies.Add(netCoreAppDependency);
            }
            return new ScriptDependencyContext(scriptDependencies.ToArray());
        }

        private static bool IsAssembly(string file)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/assembly/identify
            try
            {
                AssemblyName.GetAssemblyName(file);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static LockFileTarget GetLockFileTarget(LockFile lockFile)
        {
            if (lockFile.Targets.Count < 2)
            {
                string message = $@"The lock file {lockFile.Path} does not contain a runtime target.
 Make sure that the project file was restored using a RID (runtime identifier).";
                throw new InvalidOperationException(message);
            }

            return lockFile.Targets[1];
        }

        private LockFile GetLockFile(string pathToAssetsFile)
        {
            var lockFile = LockFileUtilities.GetLockFile(pathToAssetsFile, _nuGetLogger);
            if (lockFile == null)
            {
                string message = $@"Unable to read lockfile {pathToAssetsFile}.
Make sure that the file exists and that it is a valid 'project.assets.json' file.";
                throw new InvalidOperationException(message);
            }

            return lockFile;
        }

        private ScriptDependency CreateScriptDependency(string name, string version, FallbackPackagePathResolver packagePathResolver, LockFileTargetLibrary targetLibrary)
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
            foreach (var runtimeTarget in targetLibrary.NativeLibraries.Where(lfi => !lfi.Path.EndsWith("_._")))
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