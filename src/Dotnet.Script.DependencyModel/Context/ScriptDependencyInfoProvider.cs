using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    /// Represents a class that is capable of providing
    /// the <see cref="DependencyContext"/> for a given project file (csproj).
    /// </summary>
    public class ScriptDependencyInfoProvider
    {
        private readonly IRestorer _restorer;
        private readonly Logger _logger;

        public ScriptDependencyInfoProvider(IRestorer restorer, LogFactory logFactory)
        {
            _restorer = restorer;
            _logger = logFactory.CreateLogger<ScriptDependencyInfoProvider>();
        }

        /// <summary>
        /// Gets the <see cref="DependencyContext"/> for the project file.
        /// </summary>
        /// <param name="pathToProjectFile">The path to the project file.</param>
        /// <returns>The <see cref="DependencyContext"/> for a given project file (csproj).</returns>
        public ScriptDependencyInfo GetDependencyInfo(string pathToProjectFile, string[] packagesSources)
        {
            Restore(pathToProjectFile, packagesSources);
            var context = ReadDependencyContext(pathToProjectFile);
            var nugetPackageFolders = GetNuGetPackageFolders(pathToProjectFile);
            return new ScriptDependencyInfo(context, nugetPackageFolders);
        }

        public ScriptDependencyInfo GetDependencyInfo(string dllPath)
        {
            var context = ReadDependencyContext(dllPath);
            var nugetPackageFolders = GetNuGetPackageFolders(dllPath);
            return new ScriptDependencyInfo(context, nugetPackageFolders);
        }

        private void Restore(string pathToProjectFile, string[] packageSources)
        {
            if (_restorer.CanRestore)
            {
                _restorer.Restore(pathToProjectFile, packageSources);
                return;
            }
        }

        private DependencyContext ReadDependencyContext(string pathToProjectFile)
        {
            _logger.Debug($"Reading dependency context from {pathToProjectFile}");

            var pathToAssetsFiles = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");

            using (FileStream fs = new FileStream(pathToAssetsFiles, FileMode.Open, FileAccess.Read))
            {
                // https://github.com/dotnet/core-setup/blob/master/src/managed/Microsoft.Extensions.DependencyModel/DependencyContextJsonReader.cs
                using (var contextReader = new DependencyContextJsonReader())
                {
                    return contextReader.Read(fs);
                }
            }
        }

        private string[] GetNuGetPackageFolders(string pathToProjectFile)
        {
            var pathToObjFolder = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj");
            var pathToPropsFile = Directory.GetFiles(pathToObjFolder, "*.csproj.nuget.g.props").Single();
            var document = XDocument.Load(pathToPropsFile);
            var packageFolders = document.Descendants().Single(d => d.Name.LocalName == "NuGetPackageFolders").Value;
            _logger.Debug($"Resolved NuGet package folders: {packageFolders}");
            return packageFolders.Split(';');
        }
    }
}