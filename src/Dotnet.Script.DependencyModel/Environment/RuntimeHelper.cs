using System.IO;
using System.Text.RegularExpressions;
using Microsoft.DotNet.PlatformAbstractions;

namespace Dotnet.Script.DependencyModel.Environment
{
    public static class RuntimeHelper
    {
        private static readonly Regex RuntimeMatcher =
            new Regex($"{GetPlatformIdentifier()}.*-{GetProcessArchitecture()}");

        public static string GetPlatformIdentifier()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Darwin) return "osx";
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux) return "linux";
            return "win";            
        }

        public static bool IsWindows()
        {
            return GetPlatformIdentifier() == "win";
        }

        private static string GetDotnetBinaryPath()
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
       
        public static string GetPathToNuGetStoreFolder()
        {            
            var processArchitecture = GetProcessArchitecture();
            var storePath = Path.Combine(GetDotnetBinaryPath(), "store", processArchitecture, "netcoreapp2.0");
            return storePath;
        }


        private static string GetProcessArchitecture()
        {
            return RuntimeEnvironment.RuntimeArchitecture;
            
        }

        public static string GetRuntimeIdentifier()
        {            
            return RuntimeEnvironment.GetRuntimeIdentifier();            
        }       

        internal static bool AppliesToCurrentRuntime(string runtime)
        {
            //var regex = new Regex("win.*-x64", RegexOptions.IgnoreCase);
            return string.IsNullOrWhiteSpace(runtime) || RuntimeMatcher.IsMatch(runtime);
        }
    }
}