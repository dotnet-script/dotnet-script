using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Parsing;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public interface IScriptProjectProvider
    {
        string CreateProject(string targetDirectory);
    }

    public class ScriptProjectProvider : IScriptProjectProvider
    {
        private readonly ScriptParser scriptParser;

        private readonly Action<bool, string> _logger;
        /// <summary>        
        /// Initializes a new instance of the <see cref="ScriptProjectProvider"/> class.
        /// </summary>        
        /// <param name="scriptParser">The <see cref="IScriptParser"/> that is responsible for parsing NuGet references from script files.</param>
        public ScriptProjectProvider(ScriptParser scriptParser, Action<bool, string > logger)
        {
            _logger = logger;
            this.scriptParser = scriptParser;
        }

        public string CreateProject(string targetDirectory)
        {
            var pathToCsProj = Directory.GetFiles(targetDirectory, "*.csproj").FirstOrDefault();
            if (pathToCsProj != null)
            {
                _logger.Verbose($"Found runtime context for '{pathToCsProj}'.");
                return pathToCsProj;
            }

            _logger.Verbose("Unable to find project context for CSX files. Will default to non-context usage.");

            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories);
            var parseresult = scriptParser.ParseFrom(csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new DependencyModel.ProjectSystem.ProjectFile();

            foreach (var packageReference in parseresult.PackageReferences)
            {
                projectFile.AddPackageReference(packageReference);
            }

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