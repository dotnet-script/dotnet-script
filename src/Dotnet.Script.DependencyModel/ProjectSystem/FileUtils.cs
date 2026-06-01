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
                var driveLetter = pathRoot;
                if (pathRoot.StartsWith(@"\\"))
                {
                    // UNC path: \\server\share\... -> extract server name from pathRoot for cache isolation
                    // pathRoot on Windows = \\server-dev\SHARE\ (full UNC root)
                    // We must extract the server name from pathRoot, not from targetDirectoryWithoutRoot
                    // (which contains the path after the share, e.g. "Projects\app").
                    // Example: targetDirectory = \\server-dev\SHARE\Projects\app
                    //   pathRoot                     = \\server-dev\SHARE\
                    //   targetDirectoryWithoutRoot   = Projects\app
                    //   serverName                   = server-dev (extracted from pathRoot)
                    // Final cache path uses serverName as the root so \\server-dev and \\server-prod
                    // no longer collide in the cache.
                    var uncWithoutLeadingSlashes = pathRoot.TrimStart('\\');
                    var serverNameEnd = uncWithoutLeadingSlashes.IndexOf('\\');
                    var serverName = serverNameEnd >= 0
                        ? uncWithoutLeadingSlashes.Substring(0, serverNameEnd)
                        : uncWithoutLeadingSlashes;

                    // Re-root the path with the server name. targetDirectoryWithoutRoot is preserved
                    // (e.g. "Projects\app") — it already excludes pathRoot.
                    targetDirectoryWithoutRoot = Path.Combine(serverName, targetDirectoryWithoutRoot);
                    driveLetter = "UNC";
                }
                else
                {
                    // Regular drive letter: strip trailing backslash so Path.Combine works correctly
                    driveLetter = driveLetter.TrimEnd(new char[] { '\\' });
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