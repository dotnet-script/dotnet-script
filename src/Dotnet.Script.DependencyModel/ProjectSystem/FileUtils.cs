using Dotnet.Script.DependencyModel.Environment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string CreateTempFolder(string targetDirectory, string temporaryDirectoryRoot = null)
        {
            string pathToProjectDirectory = GetPathToTempFolder(targetDirectory, temporaryDirectoryRoot);

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;
        }

        public static string GetPathToTempFolder(string targetDirectory, string temporaryDirectoryRoot = null)
        {
            if (!Path.IsPathRooted(targetDirectory))
            {
                throw new ArgumentOutOfRangeException(nameof(targetDirectory), "Must be a root path");
            }

            var tempDirectory = !string.IsNullOrEmpty(temporaryDirectoryRoot) ? temporaryDirectoryRoot : Path.GetTempPath();
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
            var pathToProjectDirectory = Path.Combine(tempDirectory, "scripts", targetDirectoryWithoutRoot);
            return pathToProjectDirectory;
        }
    }
}
