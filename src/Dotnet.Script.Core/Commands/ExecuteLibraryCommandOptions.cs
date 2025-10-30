#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteLibraryCommandOptions
    {
        public ExecuteLibraryCommandOptions(string libraryPath, string[] arguments, string cachePath, bool noCache)
        {
            LibraryPath = libraryPath;
            Arguments = arguments;
            CachePath = cachePath;
            NoCache = noCache;
        }

        public string LibraryPath { get; }
        public string[] Arguments { get; }
        public string CachePath { get; }
        public bool NoCache { get; }

#if NETCOREAPP
#nullable enable
        /// <summary>
        /// Gets or sets a custom assembly load context to use for script execution.
        /// </summary>
        public AssemblyLoadContext? AssemblyLoadContext { get; init; }
#nullable restore
#endif
    }
}