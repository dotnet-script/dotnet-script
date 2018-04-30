using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.DotNet.PlatformAbstractions;

namespace Dotnet.Script.DependencyModel.Environment
{
    public class ScriptEnvironment
    {
        private static readonly Lazy<ScriptEnvironment> _default = new Lazy<ScriptEnvironment>(() => new ScriptEnvironment());

        private readonly Lazy<string> _targetFramework;

        private readonly Lazy<string> _installLocation;

        private readonly Lazy<string> _platformIdentifier;

        private readonly Lazy<string> _runtimeIdentifier;

        private readonly Lazy<bool> _isWindows;

        private readonly Lazy<string> _nuGetStoreFolder;

        public static ScriptEnvironment Default => _default.Value;

        private ScriptEnvironment()
        {
            _targetFramework = new Lazy<string>(GetNetCoreAppVersion);
            _installLocation = new Lazy<string>(GetInstallLocation);
            _platformIdentifier = new Lazy<string>(GetPlatformIdentifier);
            _runtimeIdentifier = new Lazy<string>(GetRuntimeIdentifier);
            _isWindows = new Lazy<bool>(() => PlatformIdentifier == "win");
            _nuGetStoreFolder = new Lazy<string>(GetPathToNuGetStoreFolder);
        }

        public bool IsWindows => _isWindows.Value;

        public string PlatformIdentifier => _platformIdentifier.Value;

        public string RuntimeIdentifier => _runtimeIdentifier.Value;

        public string TargetFramework => _targetFramework.Value;

        public string InstallLocation => _installLocation.Value;

        public string ProccessorArchitecture => RuntimeEnvironment.RuntimeArchitecture;

        public string NuGetStoreFolder => _nuGetStoreFolder.Value;

        private static string GetPlatformIdentifier()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Darwin) return "osx";
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux) return "linux";
            return "win";
        }

        private static string GetNetCoreAppVersion()
        {
            // https://github.com/dotnet/BenchmarkDotNet/blob/94863ab4d024eca04d061423e5aad498feff386b/src/BenchmarkDotNet/Portability/RuntimeInformation.cs#L156 

            var codeBase = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.CodeBase;
            var pattern = @"^.*Microsoft\.NETCore\.App\/(\d\.\d)";
            var match = Regex.Match(codeBase, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to determine netcoreapp version");
            }
            var version = match.Groups[1].Value;
            return $"netcoreapp{version}";
        }

        private static string GetInstallLocation()
        {
            return Path.GetDirectoryName(new Uri(typeof(ScriptEnvironment).GetTypeInfo().Assembly.CodeBase).LocalPath);
        }

        private string GetDotnetBinaryPath()
        {
            string basePath;
            if (IsWindows)
            {
                basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            }
            else
            {
                basePath = "usr/local/share";
            }
            return Path.Combine(basePath, "dotnet");
        }

        private string GetPathToNuGetStoreFolder()
        {
            var processArchitecture = GetProcessArchitecture();
            var storePath = Path.Combine(GetDotnetBinaryPath(), "store", processArchitecture, TargetFramework);
            return storePath;
        }

        private static string GetProcessArchitecture()
        {
            return RuntimeEnvironment.RuntimeArchitecture;
        }

        private static string GetRuntimeIdentifier()
        {
            var platformIdentifier = GetPlatformIdentifier();
            if (platformIdentifier == "osx" || platformIdentifier == "linux")
            {
                return $"{platformIdentifier}-{GetProcessArchitecture()}";
            }
            var runtimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
            return runtimeIdentifier;
        }
    }
}