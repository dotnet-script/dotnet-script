using System;
using System.IO;
using System.Linq;
using System.Net;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{    
    public class ScriptProjectProvider 
    {
        private readonly ScriptParser _scriptParser;

        private readonly Logger _logger;
        
        private ScriptProjectProvider(ScriptParser scriptParser, LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<ScriptProjectProvider>();
            _scriptParser = scriptParser;
        }

        public ScriptProjectProvider(LogFactory logFactory) : this(new ScriptParser(logFactory), logFactory)
        {
        }

        public string CreateProject(string targetDirectory, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false)
        {
            var pathToProjectFile = Directory.GetFiles(targetDirectory, "*.csproj").FirstOrDefault();
            if (pathToProjectFile == null && !enableNuGetScriptReferences)
            {
                return null;
            }

            _logger.Debug($"Creating project file for *.csx files found in {targetDirectory} using {defaultTargetFramework} as the default framework." );
            
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories);
            var parseresult = _scriptParser.ParseFrom(csxFiles);

            pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new ProjectFile();

            foreach (var packageReference in parseresult.PackageReferences)
            {
                projectFile.AddPackageReference(packageReference);
            }

            projectFile.SetTargetFramework(parseresult.TargetFramework ?? defaultTargetFramework);

            projectFile.Save(pathToProjectFile);
            _logger.Debug($"Project file saved to {pathToProjectFile}");
            CopyNuGetConfigFile(targetDirectory, Path.GetDirectoryName(pathToProjectFile));
            return pathToProjectFile;
        }

        private void CopyNuGetConfigFile(string targetDirectory, string pathToProjectFileFolder)
        {
            var pathToNuGetConfigFile = Path.Combine(targetDirectory, "NuGet.Config");
            if (File.Exists(pathToNuGetConfigFile))
            {
                var pathToDestinationNuGetConfigFile = Path.Combine(pathToProjectFileFolder, "NuGet.Config");
                _logger.Debug($"Copying {pathToNuGetConfigFile} to {pathToDestinationNuGetConfigFile}");
                File.Copy(pathToNuGetConfigFile, Path.Combine(pathToProjectFileFolder, "NuGet.Config"), true);
            }
        }

        private static string GetPathToProjectFile(string targetDirectory)
        {
            var tempDirectory = Path.GetTempPath();
            var pathRoot = Path.GetPathRoot(targetDirectory);
            var targetDirectoryWithoutRoot = targetDirectory.Substring(pathRoot.Length);
            if (pathRoot.Length > 0 && RuntimeHelper.IsWindows())
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

            var pathToProjectFile = Path.Combine(pathToProjectDirectory, "script.csproj");
            return pathToProjectFile;
        }
    }
}