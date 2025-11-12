using Microsoft.CodeAnalysis;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteScriptCommandOptions
    {
        public ExecuteScriptCommandOptions(ScriptFile file, string[] arguments, OptimizationLevel optimizationLevel, string[] packageSources, bool isInteractive, string cachePath, bool noCache)
        {
            File = file;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            IsInteractive = isInteractive;
            CachePath = cachePath;
            NoCache = noCache;
        }

        public ScriptFile File { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public bool IsInteractive { get; }
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