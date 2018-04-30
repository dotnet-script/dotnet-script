using Dotnet.Script.DependencyModel.Environment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string ReadFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string CreateTempFolder(string targetDirectory)
        {
            if (!Path.IsPathRooted(targetDirectory))
            {
                throw new ArgumentOutOfRangeException(nameof(targetDirectory), "Must be a root path");
            }

            var tempDirectory = Path.GetTempPath();
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

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;
        }
    }
}
