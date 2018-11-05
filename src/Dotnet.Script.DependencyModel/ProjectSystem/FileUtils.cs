using Dotnet.Script.DependencyModel.Environment;
using System;
using System.IO;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public static class FileUtils
    {
        public static string CreateTempFolder(string targetDirectory)
        {
            string pathToProjectDirectory = GetPathToTempFolder(targetDirectory);

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
            return pathToProjectDirectory;
        }

        public static void RemoveDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            NormalizeAttributes(path);

            foreach (string directory in Directory.GetDirectories(path))
            {
                RemoveDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }

            void NormalizeAttributes(string directoryPath)
            {
                string[] filePaths = Directory.GetFiles(directoryPath);
                string[] subdirectoryPaths = Directory.GetDirectories(directoryPath);

                foreach (string filePath in filePaths)
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }
                foreach (string subdirectoryPath in subdirectoryPaths)
                {
                    NormalizeAttributes(subdirectoryPath);
                }
                File.SetAttributes(directoryPath, FileAttributes.Normal);
            }
        }
    }
}
