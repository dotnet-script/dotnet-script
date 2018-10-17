using System.IO;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    ///
    /// </summary>
    public class CachedRestorer : IRestorer
    {
        private readonly IRestorer _restorer;
        private readonly Logger _logger;

        public CachedRestorer(IRestorer restorer, LogFactory logFactory)
        {
            _restorer = restorer;
            _logger = logFactory.CreateLogger<CachedRestorer>();
        }

        public bool CanRestore => _restorer.CanRestore;

        public void Restore(string pathToProjectFile, string[] packageSources)
        {
            var projectFile = new ProjectFile(File.ReadAllText(pathToProjectFile));
            var pathToCachedProjectFile = $"{pathToProjectFile}.cache";
            if (File.Exists(pathToCachedProjectFile))
            {
                _logger.Debug($"Found cached csproj file at {pathToCachedProjectFile}");
                var cachedProjectFile = new ProjectFile(File.ReadAllText(pathToCachedProjectFile));
                if (projectFile.Equals(cachedProjectFile))
                {
                    _logger.Debug($"Skipping restore. {pathToProjectFile} and {pathToCachedProjectFile} are identical.");
                    return;
                }
                else
                {
                    RestoreAndCacheProjectFile();
                }
            }
            else
            {
                RestoreAndCacheProjectFile();
            }

            void RestoreAndCacheProjectFile()
            {
                _restorer.Restore(pathToProjectFile, packageSources);
                if (projectFile.IsCacheable)
                {
                    projectFile.Save(pathToCachedProjectFile);
                }
                else
                {
                    _logger.Warning($"Unable to cache {pathToProjectFile}. For caching and optimal performance, ensure that the script(s) references Nuget packages with a pinned version.");
                }
            }
        }
    }
}