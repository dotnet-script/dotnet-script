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
        private readonly string _cachePath;
        private readonly Logger _logger;

        private ScriptProjectProvider(ScriptParser scriptParser, ScriptFilesResolver scriptFilesResolver, LogFactory logFactory, string cachePath, ScriptEnvironment scriptEnvironment)
        {
            _logger = logFactory.CreateLogger<ScriptProjectProvider>();
            _cachePath = cachePath;
            _scriptParser = scriptParser;
            _scriptFilesResolver = scriptFilesResolver;
            _scriptEnvironment = scriptEnvironment;
        }

        public ScriptProjectProvider(LogFactory logFactory, string cachePath) : this(new ScriptParser(logFactory), new ScriptFilesResolver(), logFactory, cachePath, ScriptEnvironment.Default)
        {
        }

        public ProjectFileInfo CreateProjectForRepl(string code, string targetDirectory, string defaultTargetFramework = "net46")
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
            var pathToProjectFile = GetPathToProjectFile(targetDirectory, defaultTargetFramework);
            var projectFile = new ProjectFile();

            foreach (var packageReference in allPackageReferences)
            {
                projectFile.PackageReferences.Add(packageReference);
            }

            projectFile.TargetFramework = defaultTargetFramework;
            projectFile.Sdk = parseResultFromCode.Sdk;

            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

            return new ProjectFileInfo(pathToProjectFile, NuGetUtilities.GetNearestConfigPath(targetDirectory));
        }

        private void LogProjectFileInfo(string pathToProjectFile)
        {
            _logger.Debug($"Project file saved to {pathToProjectFile}");
            var content = File.ReadAllText(pathToProjectFile);
            _logger.Debug(content);
        }

        public ProjectFileInfo CreateProject(string targetDirectory, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false)
        {
            return CreateProject(targetDirectory, Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories), defaultTargetFramework, enableNuGetScriptReferences);
        }

        public ProjectFileInfo CreateProject(string targetDirectory, IEnumerable<string> scriptFiles, string defaultTargetFramework = "net46", bool enableNuGetScriptReferences = false)
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

        public ProjectFileInfo CreateProjectForScriptFile(string scriptFile)
        {
            _logger.Debug($"Creating project file for {scriptFile}");
            var scriptFiles = _scriptFilesResolver.GetScriptFiles(scriptFile);
            return SaveProjectFileFromScriptFiles(Path.GetDirectoryName(scriptFile), _scriptEnvironment.TargetFramework, scriptFiles.ToArray());
        }

        private ProjectFileInfo SaveProjectFileFromScriptFiles(string targetDirectory, string defaultTargetFramework, string[] csxFiles)
        {
            ProjectFile projectFile = CreateProjectFileFromScriptFiles(defaultTargetFramework, csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory, defaultTargetFramework);
            projectFile.Save(pathToProjectFile);

            LogProjectFileInfo(pathToProjectFile);

            return new ProjectFileInfo(pathToProjectFile, NuGetUtilities.GetNearestConfigPath(targetDirectory));
        }

        public ProjectFile CreateProjectFileFromScriptFiles(string defaultTargetFramework, string[] csxFiles)
        {
            var parseResult = _scriptParser.ParseFromFiles(csxFiles);

            var projectFile = new ProjectFile();

            foreach (var packageReference in parseResult.PackageReferences)
            {
                projectFile.PackageReferences.Add(packageReference);
            }

            projectFile.TargetFramework = defaultTargetFramework;
            projectFile.Sdk = parseResult.Sdk;
            return projectFile;
        }


        public string GetPathToProjectFile(string targetDirectory, string targetFramework, string projectName = null)
        {
            projectName ??= "script";
            var projectFileName = projectName + ".csproj";
            var pathToProjectDirectory = FileUtils.CreateTempFolder(targetDirectory, _cachePath, targetFramework);
            var pathToProjectFile = Path.Combine(pathToProjectDirectory, projectFileName);
            return pathToProjectFile;
        }

        public bool IsTempPath(string path)
        {
            return path.StartsWith(FileUtils.GetTempPath()) || (!string.IsNullOrEmpty(_cachePath) && path.StartsWith(_cachePath));
        }
    }
}