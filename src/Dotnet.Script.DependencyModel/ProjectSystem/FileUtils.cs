using Dotnet.Script.DependencyModel.Environment;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string CreateTempFolder(string targetDirectory, string targetFramework)
        {
            string pathToProjectDirectory = Path.Combine(GetPathToTempFolder(targetDirectory), targetFramework);

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;
        }

        public static string GetPathToTempFolder(string targetDirectory)
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

        private static string GetTempPath()
        {
            var userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(userFolder, ".cache");
            }
            else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(userFolder, "Library/Caches/");
            }

            return Path.GetTempPath();
        }
    }
}
