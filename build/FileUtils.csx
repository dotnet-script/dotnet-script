#load "Logger.csx"
#load "Command.csx"
using System.Text.RegularExpressions;


public static class FileUtils
{
    public static void ReplaceInFile(string pattern, string value, string pathToFile)
    {
        var source = ReadFile(pathToFile);
        var replacedSource = Regex.Replace(source, pattern, value);
        WriteFile(pathToFile, replacedSource);
    }


    public static string ReadFile(string pathToFile)
    {
        using (var fileStream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            using (var reader = new StreamReader(fileStream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static void WriteFile(string pathToFile, string content)
    {
        using (var fileStream = new FileStream(pathToFile, FileMode.Create))
        {
            using (var writer = new StreamWriter(fileStream))
            {
                writer.Write(content);
            }
        }
    }

    public static string FindFile(string path, string filePattern)
    {
        Logger.Log($"Looking for {filePattern} in {path}");
        string[] pathsToFile = Directory.GetFiles(path, filePattern, SearchOption.AllDirectories).ToArray();
        if (pathsToFile.Length > 1)
        {
            Logger.Log("Found multiple files");
            var files = pathsToFile.Select(p => new FileInfo(p));
            var file = files.OrderBy(f => f.LastWriteTime).Last();
            Logger.Log($"Choosing {file.FullName}");
            return file.FullName;
        }
        else
        if (pathsToFile.Length == 0)
        {
            Logger.Log($"File {filePattern} not found in {path}");
            return null;
        }
        Logger.Log($"Found {pathsToFile[0]}");
        return pathsToFile[0];
    }

    public static string FindDirectory(string path, string filePattern)
    {
        string pathToFile = FindFile(path, filePattern);
        return Path.GetDirectoryName(pathToFile);
    }

    public static void RemoveDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        // http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
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
    }

    public static void CreateDirectory(string directory)
    {
        RemoveDirectory(directory);
        Directory.CreateDirectory(directory);
    }

    public static void RoboCopy(string source, string destination, string arguments = null)
    {
        if (!Directory.Exists(source))
        {
            throw new InvalidOperationException(string.Format("The directory {0} does not exist", source));
        }

        Command.Execute("robocopy", string.Format("{0} {1} {2}", source, destination, arguments));
    }

    public static void CopySolution(string pathToSolutionFolder, string pathToDestinationFolder)
    {
        if (Directory.Exists(pathToDestinationFolder))
        {
            CreateDirectory(pathToDestinationFolder);
        }
        
        RoboCopy(pathToSolutionFolder, pathToDestinationFolder, "/e /XD bin obj .vs NuGet TestResults packages");
    }
}
