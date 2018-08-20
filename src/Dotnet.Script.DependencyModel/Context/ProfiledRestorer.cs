using System.Diagnostics;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.Context
{
    public class ProfiledRestorer : IRestorer
    {
        private readonly IRestorer _restorer;
        private readonly Logger _logger;

        public ProfiledRestorer(IRestorer restorer, LogFactory logFactory)
        {
            _restorer = restorer;
            _logger = logFactory.CreateLogger<ProfiledRestorer>();
        }

        public bool CanRestore => _restorer.CanRestore;

        public void Restore(string pathToProjectFile, string[] packageSources)
        {
            var stopwatch = Stopwatch.StartNew();
            _restorer.Restore(pathToProjectFile, packageSources);
            _logger.Debug($"Restoring {pathToProjectFile} took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
