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