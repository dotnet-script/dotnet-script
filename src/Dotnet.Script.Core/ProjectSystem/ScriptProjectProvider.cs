using System.IO;
using System.Runtime.InteropServices;
using NuGet.Common;

namespace Dotnet.Script.Core.ProjectSystem
{
    public interface IScriptProjectProvider
    {
        string CreateProject(string targetDirectory);
    }

    public class ScriptProjectProvider : IScriptProjectProvider
    {
        private readonly ScriptParser scriptParser;

        private readonly ScriptLogger _logger;
        /// <summary>        
        /// Initializes a new instance of the <see cref="ScriptProjectProvider"/> class.
        /// </summary>        
        /// <param name="scriptParser">The <see cref="IScriptParser"/> that is responsible for parsing NuGet references from script files.</param>
        public ScriptProjectProvider(ScriptParser scriptParser, ScriptLogger logger)
        {
            _logger = logger;
            this.scriptParser = scriptParser;
        }

        public string CreateProject(string targetDirectory)
        {
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories);
            var parseresult = scriptParser.ParseFrom(csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
            var projectFile = new ProjectFile();

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