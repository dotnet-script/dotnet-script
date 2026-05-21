using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dotnet.Script.DependencyModel.Environment;
using SysEnvironment = System.Environment;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string CreateTempFolder(string targetDirectory, string cachePath, string targetFramework)
        {
            string pathToProjectDirectory = Path.Combine(GetPathToScriptTempFolder(targetDirectory, cachePath), targetFramework);

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;
        }

        public static string GetPathToScriptTempFolder(string targetDirectory, string cachePath)
        {
            if (!Path.IsPathRooted(targetDirectory))
            {
                throw new ArgumentOutOfRangeException(nameof(targetDirectory), "Must be a root path");
            }

            var tempDirectory =
                string.IsNullOrEmpty(cachePath) ? GetTempPath() :
                Path.IsPathRooted(cachePath) ? cachePath :
                Path.Combine(Directory.GetCurrentDirectory(), cachePath);

            var pathRoot = Path.GetPathRoot(targetDirectory);
            var targetDirectoryWithoutRoot = targetDirectory.Substring(pathRoot.Length);
            if (pathRoot.Length > 0 && (ScriptEnvironment.Default.IsWindows || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)))
            {
                var driveLetter = pathRoot.Substring(0, 1);
                if (driveLetter == "\\")
                {
                    // UNC path: \\server\share\... -> extract server name for cache isolation
                    // Before: server-dev\SHARE\Projects\app
                    // After trimming \: SHARE\Projects\app
                    targetDirectoryWithoutRoot = targetDirectoryWithoutRoot.TrimStart(new char[] { '\\' });

                    // Split on backslash to get server name (first component) and rest
                    var slashIndex = targetDirectoryWithoutRoot.IndexOf('\\');
                    string serverName;
                    string remainder;
                    if (slashIndex >= 0)
                    {
                        serverName = targetDirectoryWithoutRoot.Substring(0, slashIndex);
                        remainder = targetDirectoryWithoutRoot.Substring(slashIndex + 1);
                    }
                    else
                    {
                        // No backslash at all — bare UNC root like \\server\share (without trailing slash)
                        serverName = targetDirectoryWithoutRoot;
                        remainder = "";
                    }

                    // Use server name as the cache path root so different servers get different caches
                    // e.g. server-dev and server-prod produce distinct cache trees
                    targetDirectoryWithoutRoot = remainder.Length > 0
                        ? Path.Combine(serverName, remainder)
                        : serverName;
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
                // if the path is not absolute, make it relative to the current folder
                if (!Path.IsPathRooted(cachePath)) 
                {
                    cachePath = Path.Combine(Directory.GetCurrentDirectory(), cachePath);
                }
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