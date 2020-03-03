using System;
using System.IO;

namespace Dotnet.Script.Shared.Tests
{
    public class FileUtils
    {
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