using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    /// Represents a class that is capable of providing 
    /// the <see cref="DependencyContext"/> for a given project file (csproj).
    /// </summary>
    public class ScriptDependencyInfoProvider 
    {
        private readonly IRestorer[] _restorers;
        private readonly Action<bool, string> _logger;

        public ScriptDependencyInfoProvider(IRestorer[] restorers, Action<bool, string> logger)
        {
            _restorers = restorers;
            _logger = logger;
        }

        /// <summary>
        /// Gets the <see cref="DependencyContext"/> for the project file.
        /// </summary>
        /// <param name="pathToProjectFile">The path to the project file.</param>
        /// <returns>The <see cref="DependencyContext"/> for a given project file (csproj).</returns>
        public ScriptDependencyInfo GetDependencyInfo(string pathToProjectFile)
        {            
            Restore(pathToProjectFile);
            var context =  ReadDependencyContext(pathToProjectFile);
            var nugetPackageFolders = GetNuGetPackageFolders(pathToProjectFile);
            return new ScriptDependencyInfo(context, nugetPackageFolders);
        }

        private void Restore(string pathToProjectFile)
        {
            foreach (var restorer in _restorers)
            {
                if (restorer.CanRestore)
                {
                    restorer.Restore(pathToProjectFile);
                    return;
                }
            }
        }

        private DependencyContext ReadDependencyContext(string pathToProjectFile)
        {
            _logger.Verbose($"Reading dependency context from {pathToProjectFile}");

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
            _logger.Verbose($"Resolved NuGet package folders: {packageFolders}");
            return packageFolders.Split(';');
        }
    }
}