using System.Threading.Tasks;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Represents a class that is capable of providing version information.
    /// </summary>
    public interface IVersionProvider
    {
        /// <summary>
        /// Gets the latest available version.
        /// </summary>
        /// <returns><see cref="VersionInfo"/>.</returns>
        Task<VersionInfo> GetLatestVersion();

        /// <summary>
        /// Gets the current version.
        /// </summary>
        /// <returns></returns>
        VersionInfo GetCurrentVersion();
    }
}