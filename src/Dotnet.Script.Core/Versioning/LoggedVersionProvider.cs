using System;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// A <see cref="IVersionProvider"/> decorator that logs exceptions
    /// reletated to getting version information.
    /// </summary>
    public class LoggedVersionProvider : IVersionProvider
    {
        private readonly IVersionProvider _versionProvider;
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggedVersionProvider"/> class.
        /// </summary>
        /// <param name="versionProvider">The decorated <see cref="IVersionProvider"/>.</param>
        /// <param name="logFactory">The <see cref="LogFactory"/> to be used for logging.</param>
        public LoggedVersionProvider(IVersionProvider versionProvider, LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<LoggedVersionProvider>();
            _versionProvider = versionProvider;
        }

        /// <summary>
        /// Initializes a new instance of the LoggedVersionProvider using the default dependencies.
        /// </summary>
        /// <param name="logFactory">The <see cref="LogFactory"/> to be used for logging.</param>
        public LoggedVersionProvider(LogFactory logfactory)
            :this
            (
                new VersionProvider(),
                logfactory
            )
        { }

        /// <inheritdoc/>
        public VersionInfo GetCurrentVersion()
        {
            try
            {
                return _versionProvider.GetCurrentVersion();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to retrieve information about the current version", ex);
                return VersionInfo.UnResolved;
            }
        }

        /// <inheritdoc/>
        public async Task<VersionInfo> GetLatestVersion()
        {
            try
            {
                return  await _versionProvider.GetLatestVersion();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to retrieve information about the latest version", ex);
                return VersionInfo.UnResolved;
            }
        }
    }
}