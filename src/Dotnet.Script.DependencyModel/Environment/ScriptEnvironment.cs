using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.DotNet.PlatformAbstractions;

namespace Dotnet.Script.DependencyModel.Environment
{
    public class ScriptEnvironment
    {
        public static readonly ScriptEnvironment Default = new ScriptEnvironment();

        private readonly Lazy<string> _targetFramework;

        private readonly Lazy<string> _installLocation;

        private readonly Lazy<string> _platformIdentifier;

        private readonly Lazy<string> _runtimeIdentifier;

        private readonly Lazy<bool> _isWindows;

        private readonly Lazy<string> _nuGetStoreFolder;

        private readonly Lazy<DotnetVersion> _netCoreVersion;

        private string _overrriddenTargetFramework;

        private ScriptEnvironment()
        {
            _netCoreVersion = new Lazy<DotnetVersion>(GetNetCoreAppVersion);
            _targetFramework = new Lazy<string>(() => _netCoreVersion.Value == DotnetVersion.Unknown ? "net472" : _netCoreVersion.Value.Tfm);
            _installLocation = new Lazy<string>(GetInstallLocation);
            _platformIdentifier = new Lazy<string>(GetPlatformIdentifier);
            _runtimeIdentifier = new Lazy<string>(GetRuntimeIdentifier);
            _isWindows = new Lazy<bool>(() => PlatformIdentifier == "win");
            _nuGetStoreFolder = new Lazy<string>(GetPathToNuGetStoreFolder);
        }

        public bool IsWindows => _isWindows.Value;

        public string PlatformIdentifier => _platformIdentifier.Value;

        public string RuntimeIdentifier => _runtimeIdentifier.Value;

        public string TargetFramework => _overrriddenTargetFramework ?? _targetFramework.Value;

        public string InstallLocation => _installLocation.Value;

        public string ProccessorArchitecture => RuntimeEnvironment.RuntimeArchitecture;

        public string NuGetStoreFolder => _nuGetStoreFolder.Value;

        public DotnetVersion NetCoreVersion => _netCoreVersion.Value;

        public bool IsNetCore => _netCoreVersion.Value != DotnetVersion.Unknown;

        public void OverrideTargetFramework(string targetFramework)
        {
            if (_targetFramework.IsValueCreated)
            {
                throw new InvalidOperationException($"Cannot override target framework because a value {_targetFramework.Value} has already been resolved and used.");
            }

            _overrriddenTargetFramework = targetFramework;
        }

        private static string GetPlatformIdentifier()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Darwin) return "osx";
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux) return "linux";
            return "win";
        }

        private static DotnetVersion GetNetCoreAppVersion()
        {
            GetNetCoreVersion();
            // https://github.com/dotnet/BenchmarkDotNet/blob/94863ab4d024eca04d061423e5aad498feff386b/src/BenchmarkDotNet/Portability/RuntimeInformation.cs#L156
            var codeBase = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.CodeBase;
            var pattern = @"^.*Microsoft\.NETCore\.App\/(\d+\.\d+)(.*?)\/";
            var match = Regex.Match(codeBase, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return DotnetVersion.Unknown;
            }
            var tfm = match.Groups[1].Value;
            var version = match.Groups[1].Value + match.Groups[2].Value;

            return new DotnetVersion(version, $"netcoreapp{tfm}");
        }

        public static string GetNetCoreVersion()
        {
            var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return null;
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

    public class DotnetVersion
    {
        public static DotnetVersion Unknown = new DotnetVersion("unknown", "unknown");

        public DotnetVersion(string version, string tfm)
        {
            Version = version;
            Tfm = tfm;

            var versionMatch = Regex.Match(input: Version, pattern: @"^(\d+)(?:\.(\d+))?");
            if (versionMatch.Success && versionMatch.Groups[1].Success)
                Major = int.Parse(versionMatch.Groups[1].Value);
            if (versionMatch.Success && versionMatch.Groups[2].Success)
                Minor = int.Parse(versionMatch.Groups[2].Value);
            if (Major >= 5)
            {
                Tfm = $"net{Major}.{Minor}";
            }
        }

        public string Version { get; }
        public string Tfm { get; }
        public int Major { get; }
        public int Minor { get; }
    }
}