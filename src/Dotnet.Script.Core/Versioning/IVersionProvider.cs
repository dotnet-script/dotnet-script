using System.Threading.Tasks;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Represents a class that is capable of providing version information.
    /// </summary>
    public interface IVersionProvider
    {
        /// <summary>
        /// Gets the version represented as a string.
        /// </summary>
        /// <returns></returns>
        Task<string> GetVersion();
    }
}