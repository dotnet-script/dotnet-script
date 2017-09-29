using System;
using System.IO;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    /// Represents a class that is capable of providing 
    /// the <see cref="DependencyContext"/> for a given project file (csproj).
    /// </summary>
    public class DependencyContextProvider 
    {
        private readonly IRestorer[] _restorers;
        private readonly Action<bool, string> _logger;

        public DependencyContextProvider(IRestorer[] restorers, Action<bool, string> logger)
        {
            _restorers = restorers;
            _logger = logger;
        }

        /// <summary>
        /// Gets the <see cref="DependencyContext"/> for the project file.
        /// </summary>
        /// <param name="pathToProjectFile">The path to the project file.</param>
        /// <returns>The <see cref="DependencyContext"/> for a given project file (csproj).</returns>
        public DependencyContext GetDependencyContext(string pathToProjectFile)
        {
            Restore(pathToProjectFile);
            return ReadDependencyContext(pathToProjectFile);
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

        private static DependencyContext ReadDependencyContext(string pathToProjectFile)
        {
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
    }
}