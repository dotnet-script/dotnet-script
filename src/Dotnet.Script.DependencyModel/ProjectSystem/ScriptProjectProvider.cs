using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using NuGet.Configuration;

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

        public ScriptProjectProvider(LogFactory logFactory, ScriptEnvironment scriptEnvironment) : this(new ScriptParser(logFactory), new ScriptFilesResolver(), logFactory, scriptEnvironment)
        {
        }

        public string CreateProjectForRepl(string code, string targetDirectory, string defaultTargetFramework = "net46")
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
            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new ProjectFile();

            foreach (var packageReference in allPackageReferences)
            {
                projectFile.PackageReferences.Add(packageReference);
            }

            projectFile.TargetFramework = parseResultFromCode.TargetFramework ?? defaultTargetFramework;

            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

            EvaluateAndGenerateNuGetConfigFile(targetDirectory, Path.GetDirectoryName(pathToProjectFile));
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
            return CreateProject(targetDirectory, Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories), defaultTargetFramework, enableNuGetScriptReferences);
        }

        public string CreateProject(string targetDirectory, IEnumerable<string> scriptFiles, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false)
        {
            if (scriptFiles == null || !scriptFiles.Any())
            {
                return null;
            }

            var pathToProjectFile = Directory.GetFiles(targetDirectory, "*.csproj").FirstOrDefault();
            if (pathToProjectFile == null && !enableNuGetScriptReferences)
            {
                return null;
            }

            _logger.Debug($"Creating project file for *.csx files found in {targetDirectory} using {defaultTargetFramework} as the default framework.");

            return SaveProjectFileFromScriptFiles(targetDirectory, defaultTargetFramework, scriptFiles.ToArray());
        }

        public string CreateProjectForScriptFile(string scriptFile)
        {
            _logger.Debug($"Creating project file for {scriptFile}");
            var scriptFiles = _scriptFilesResolver.GetScriptFiles(scriptFile);
            return SaveProjectFileFromScriptFiles(Path.GetDirectoryName(scriptFile), _scriptEnvironment.TargetFramework, scriptFiles.ToArray());
        }

        private string SaveProjectFileFromScriptFiles(string targetDirectory, string defaultTargetFramework, string[] csxFiles)
        {
            ProjectFile projectFile = CreateProjectFileFromScriptFiles(defaultTargetFramework, csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

            EvaluateAndGenerateNuGetConfigFile(targetDirectory, Path.GetDirectoryName(pathToProjectFile));
            return pathToProjectFile;
        }

        public ProjectFile CreateProjectFileFromScriptFiles(string defaultTargetFramework, string[] csxFiles)
        {
            var parseresult = _scriptParser.ParseFromFiles(csxFiles);

            var projectFile = new ProjectFile();

            foreach (var packageReference in parseresult.PackageReferences)
            {
                projectFile.PackageReferences.Add(packageReference);
            }

            projectFile.TargetFramework = parseresult.TargetFramework ?? defaultTargetFramework;
            return projectFile;
        }

        private void EvaluateAndGenerateNuGetConfigFile(string targetDirectory, string pathToProjectFileFolder)
        {
            var pathToDestinationNuGetConfigFile = Path.Combine(pathToProjectFileFolder, Settings.DefaultSettingsFileName);

            if (File.Exists(pathToDestinationNuGetConfigFile))
                File.Delete(pathToDestinationNuGetConfigFile);

            _logger.Debug($"Generating NuGet config evaluated at {targetDirectory} to {pathToDestinationNuGetConfigFile}");
            NuGetUtilities.CreateNuGetConfigFromLocation(targetDirectory, pathToProjectFileFolder);
        }

        public static string GetPathToProjectFile(string targetDirectory)
        {
            var pathToProjectDirectory = FileUtils.CreateTempFolder(targetDirectory);
            var pathToProjectFile = Path.Combine(pathToProjectDirectory, "script.csproj");
            return pathToProjectFile;
        }
    }
}