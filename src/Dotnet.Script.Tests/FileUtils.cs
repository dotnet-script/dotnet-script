using System;
using System.IO;

namespace Dotnet.Script.Tests
{
    public class FileUtils
    {
        public static void WriteFile(string path, string content)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(content);
                }
            }
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