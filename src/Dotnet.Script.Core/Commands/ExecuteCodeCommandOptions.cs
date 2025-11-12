using Microsoft.CodeAnalysis;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteCodeCommandOptions
    {
        public ExecuteCodeCommandOptions(string code, string workingDirectory, string[] arguments, OptimizationLevel optimizationLevel, string cachePath, bool noCache, string[] packageSources)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
			CachePath = cachePath;
            NoCache = noCache;
            PackageSources = packageSources;
        }

        public string Code { get; }
        public string WorkingDirectory { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string CachePath { get; }
        public bool NoCache { get; }
        public string[] PackageSources { get; }

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