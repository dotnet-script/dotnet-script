using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{    
    public class ScriptProjectProvider 
    {
        private readonly ScriptParser _scriptParser;

        private readonly Action<bool, string> _logger;        
        
        private ScriptProjectProvider(ScriptParser scriptParser, Action<bool, string > logger)
        {
            _logger = logger;            
            _scriptParser = scriptParser;
        }

        public ScriptProjectProvider(Action<bool, string> logger) : this(new ScriptParser(logger), logger)
        {            
        }       

        public string CreateProject(string targetDirectory, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false)
        {
            var pathToProjectFile = Directory.GetFiles(targetDirectory, "*.csproj").FirstOrDefault();
            if (pathToProjectFile == null && !enableNuGetScriptReferences)
            {
                return null;
            }

            _logger.Verbose($"Creating project file for *.csx files found in {targetDirectory} using {defaultTargetFramework} as the default framework." );
            
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
            _logger.Verbose($"Project file saved to {pathToProjectFile}");
            return pathToProjectFile;
        }

        private static string GetPathToProjectFile(string targetDirectory)
        {
            var tempDirectory = Path.GetTempPath();
            var pathRoot = Path.GetPathRoot(targetDirectory);
            var targetDirectoryWithoutRoot = targetDirectory.Substring(pathRoot.Length);
            if (pathRoot.Length > 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var driveLetter = pathRoot.Substring(0, 1);
                targetDirectoryWithoutRoot = Path.Combine(driveLetter, targetDirectoryWithoutRoot);
            }
            var pathToProjectJsonDirectory = Path.Combine(tempDirectory, "scripts", targetDirectoryWithoutRoot);
            if (!Directory.Exists(pathToProjectJsonDirectory))
            {
                Directory.CreateDirectory(pathToProjectJsonDirectory);
            }
            var pathToProjectJson = Path.Combine(pathToProjectJsonDirectory, "script.csproj");
            return pathToProjectJson;
        }
    }
}