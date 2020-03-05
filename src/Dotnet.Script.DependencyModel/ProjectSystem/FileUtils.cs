using Dotnet.Script.DependencyModel.Environment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SysEnvironment = System.Environment;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string CreateTempFolder(string targetDirectory, string targetFramework)
        {
            string pathToProjectDirectory = Path.Combine(GetPathToScriptTempFolder(targetDirectory), targetFramework);

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;
        }

        public static string GetPathToScriptTempFolder(string targetDirectory)
        {
            if (!Path.IsPathRooted(targetDirectory))
            {
                throw new ArgumentOutOfRangeException(nameof(targetDirectory), "Must be a root path");
            }

            var tempDirectory = GetTempPath();
            var pathRoot = Path.GetPathRoot(targetDirectory);
            var targetDirectoryWithoutRoot = targetDirectory.Substring(pathRoot.Length);
            if (pathRoot.Length > 0 && ScriptEnvironment.Default.IsWindows)
            {
                var driveLetter = pathRoot.Substring(0, 1);
                if (driveLetter == "\\")
                {
                    targetDirectoryWithoutRoot = targetDirectoryWithoutRoot.TrimStart(new char[] { '\\' });
                    driveLetter = "UNC";
                }

                targetDirectoryWithoutRoot = Path.Combine(driveLetter, targetDirectoryWithoutRoot);
            }
            var pathToProjectDirectory = Path.Combine(tempDirectory, "dotnet-script", targetDirectoryWithoutRoot);
            return pathToProjectDirectory;
        }

        public static string GetTempPath()
        {
            // prefer the custom env variable if set
            var cachePath = SysEnvironment.GetEnvironmentVariable("DOTNET_SCRIPT_CACHE_LOCATION");
            if (!string.IsNullOrEmpty(cachePath))
            {
                return cachePath;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // base dir relative to which user specific cache data files should be stored
                cachePath = SysEnvironment.GetEnvironmentVariable("XDG_CACHE_HOME");

                // if $XDG_CACHE_HOME is not set, $HOME/.cache should be used.
                if (string.IsNullOrEmpty(cachePath))
                {
                    cachePath = Path.Combine(SysEnvironment.GetFolderPath(SysEnvironment.SpecialFolder.UserProfile), ".cache");
                }

                return cachePath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(SysEnvironment.GetFolderPath(SysEnvironment.SpecialFolder.UserProfile), "Library/Caches/");
            }

            return Path.GetTempPath();
        }
    }
}
