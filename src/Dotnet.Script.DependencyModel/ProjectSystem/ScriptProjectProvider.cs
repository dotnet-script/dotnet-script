using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{    
    public class ScriptProjectProvider 
    {
        private readonly ScriptParser _scriptParser;
        private readonly ScriptFilesResolver _scriptFilesResolver;
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly Logger _logger;
        
        private ScriptProjectProvider(ScriptParser scriptParser, ScriptFilesResolver scriptFilesResolver, LogFactory logFactory, ScriptEnvironment scriptEnvironment)
        {
            _logger = logFactory.CreateLogger<ScriptProjectProvider>();
            _scriptParser = scriptParser;
            _scriptFilesResolver = scriptFilesResolver;
            _scriptEnvironment = scriptEnvironment;
        }

        public ScriptProjectProvider(LogFactory logFactory) : this(new ScriptParser(logFactory), new ScriptFilesResolver(), logFactory, ScriptEnvironment.Default)
        {
        }

        public string CreateProjectForRepl(string code, string targetDirectory, string defaultTargetFramework = "net46", string temporaryDirectory = null)
        {
            var scriptFiles = _scriptFilesResolver.GetScriptFilesFromCode(code);
            targetDirectory = Path.Combine(targetDirectory, "interactive");

            var parseResultFromCode = _scriptParser.ParseFromCode(code);
            var parseResultFromLoadedFiles = _scriptParser.ParseFromFiles(scriptFiles);
            var allPackageReferences = new HashSet<PackageReference>();

            foreach (var packageReference in parseResultFromCode.PackageReferences)
            {
                allPackageReferences.Add(packageReference);
            }

            foreach (var packageReference in parseResultFromLoadedFiles.PackageReferences)
            {
                allPackageReferences.Add(packageReference);
            }

            targetDirectory = Path.Combine(targetDirectory, "interactive");
            var pathToProjectFile = GetPathToProjectFile(targetDirectory, temporaryDirectory);
            var projectFile = new ProjectFile();

            foreach (var packageReference in allPackageReferences)
            {
                projectFile.AddPackageReference(packageReference);
            }

            projectFile.SetTargetFramework(parseResultFromCode.TargetFramework ?? defaultTargetFramework);

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

        public string CreateProject(string targetDirectory, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false, string temporaryDirectory = null)
        {
            var pathToProjectFile = Directory.GetFiles(targetDirectory, "*.csproj").FirstOrDefault();
            if (pathToProjectFile == null && !enableNuGetScriptReferences)
            {
                return null;
            }

            _logger.Debug($"Creating project file for *.csx files found in {targetDirectory} using {defaultTargetFramework} as the default framework.");

            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories);
            return CreateProjectFileFromScriptFiles(targetDirectory, defaultTargetFramework, csxFiles, temporaryDirectory);
        }

        public string CreateProjectForScriptFile(string scriptFile, string temporaryDirectory = null)
        {
            _logger.Debug($"Creating project file for {scriptFile}");
            var scriptFiles = _scriptFilesResolver.GetScriptFiles(scriptFile);
            return CreateProjectFileFromScriptFiles(Path.GetDirectoryName(scriptFile), _scriptEnvironment.TargetFramework, scriptFiles.ToArray(), temporaryDirectory);
        }

        private string CreateProjectFileFromScriptFiles(string targetDirectory, string defaultTargetFramework, string[] csxFiles, string temporaryDirectory = null)
        {
            var parseresult = _scriptParser.ParseFromFiles(csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory, temporaryDirectory);
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

        public static string GetPathToProjectFile(string targetDirectory, string temporaryDirectory)
        {
            var pathToProjectDirectory = FileUtils.CreateTempFolder(targetDirectory, temporaryDirectory);
            var pathToProjectFile = Path.Combine(pathToProjectDirectory, "script.csproj");
            return pathToProjectFile;
        }
    }
}