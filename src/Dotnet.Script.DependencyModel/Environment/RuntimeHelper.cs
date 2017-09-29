using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Environment
{
    public static class RuntimeHelper
    {
        public static string GetPlatformIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "unix";

            return "win";
        }

        public static bool IsWindows()
        {
            return GetPlatformIdentifier() == "win";
        }

        public static string GetPathToGlobalPackagesFolder()
        {
            string basePath;

            var packageDirectory = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return packageDirectory;
            }


            if (IsWindows())
            {
                basePath = System.Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = System.Environment.GetEnvironmentVariable("HOME");
            }

            if (string.IsNullOrEmpty(basePath))
            {
                return string.Empty;
            }

            return Path.Combine(basePath, ".nuget", "packages");
        }

        public static string GetPathToNuGetExecutable()
        {
            var directory = Path.GetDirectoryName(new Uri(typeof(RuntimeHelper).GetTypeInfo().Assembly.CodeBase).LocalPath);
            return Path.Combine(directory, "NuGet430.exe");
        }

        public static string GetDotnetBinaryPath()
        {
            string basePath;
            if (IsWindows())
            {
                basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            }
            else
            {
                basePath = "usr/local/share";
            }
            return Path.Combine(basePath, "dotnet");
        }

        public static string GetPathToNuGetFallbackFolder()
        {
            // Note: Read the probing paths from contenttest.runtimeconfig.dev.json
            // At least for the runtime resolver.

            return Path.Combine(GetDotnetBinaryPath(), "sdk", "NuGetFallbackFolder");
        }

        public static string GetPathToNuGetStoreFolder()
        {            
            var processArchitecture = GetProcessArchitecture();
            var storePath = Path.Combine(GetDotnetBinaryPath(), "store", processArchitecture, "netcoreapp2.0");
            return storePath;
        }


        internal static string GetProcessArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture.ToString();            
        }

        public static string GetRuntimeIdentifier()
        {
            return Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
        }

        public static string ResolveTargetFramework()
        {
            return Assembly.GetEntryAssembly().GetCustomAttributes()
                .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                .Select(x => x.FrameworkName)
                .FirstOrDefault();
        }

        internal static bool AppliesToCurrentRuntime(string runtime)
        {
            return string.IsNullOrWhiteSpace(runtime) || runtime == GetRuntimeIdentifier();
        }


    }
}