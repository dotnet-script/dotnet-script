using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Parsing;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a class that is capable of creating a 
    /// project file (csproj) based upon one or more script files 
    /// in a given directory.
    /// </summary>
    public interface IScriptProjectProvider
    {
        /// <summary>
        /// Creates a project file (csproj) based upon the 
        /// script files found in the <paramref name="targetDirectory"/>.
        /// </summary>
        /// <param name="targetDirectory">The directory containing script files.</param>
        /// <param name="defaultTargetFramework">The default target framework.</param>
        /// <returns></returns>
        string CreateProject(string targetDirectory, string defaultTargetFramework = "net46");
    }

    public class ScriptProjectProvider : IScriptProjectProvider
    {
        private readonly ScriptParser _scriptParser;

        private readonly Action<bool, string> _logger;        

        /// <summary>        
        /// Initializes a new instance of the <see cref="ScriptProjectProvider"/> class.
        /// </summary>        
        /// <param name="scriptParser">The <see cref="ScriptParser"/> that is responsible for parsing NuGet references from script files.</param>
        private ScriptProjectProvider(ScriptParser scriptParser, Action<bool, string > logger)
        {
            _logger = logger;            
            this._scriptParser = scriptParser;
        }

        public static ScriptProjectProvider Create(Action<bool, string> logger)
        {
            return new ScriptProjectProvider(new ScriptParser(logger),logger);
        }

        public string CreateProject(string targetDirectory, string defaultTargetFramework = "net46")
        {            
            _logger.Verbose($"Creating project file for *.csx files found in {targetDirectory}");

            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx", SearchOption.AllDirectories);
            var parseresult = _scriptParser.ParseFrom(csxFiles);

            var pathToProjectFile = GetPathToProjectFile(targetDirectory);
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