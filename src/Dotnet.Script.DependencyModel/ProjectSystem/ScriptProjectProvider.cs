using System.IO;
using System.Linq;
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

        public string CreateProjectForRepl(string code, string targetDirectory, string defaultTargetFramework = "net46")
        {
            var parseresult = _scriptParser.ParseFromCode(code);

            targetDirectory = Path.Combine(targetDirectory, "REPL");
            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new ProjectFile();

            foreach (var packageReference in parseresult.PackageReferences)
            {
                projectFile.AddPackageReference(packageReference);
            }

            projectFile.SetTargetFramework(parseresult.TargetFramework ?? defaultTargetFramework);

            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

            CopyNuGetConfigFile(targetDirectory, Path.GetDirectoryName(pathToProjectFile));
            return pathToProjectFile;
        }

        private void LogProjectFileInfo(string pathToProjectFile)
        {
            _logger.Debug($"Project file saved to {pathToProjectFile}");
            var content = File.ReadAllText(pathToProjectFile);
            _logger.Debug(content);
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
            var parseresult = _scriptParser.ParseFromFiles(csxFiles);

            pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new ProjectFile();

            foreach (var packageReference in parseresult.PackageReferences)
            {
                projectFile.AddPackageReference(packageReference);
            }

            projectFile.SetTargetFramework(parseresult.TargetFramework ?? defaultTargetFramework);

            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

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
            var pathToProjectDirectory = RuntimeHelper.CreateTempFolder(targetDirectory);
            var pathToProjectFile = Path.Combine(pathToProjectDirectory, "script.csproj");
            return pathToProjectFile;
        }
    }
}