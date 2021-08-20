#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteInteractiveCommandOptions
    {
        public ExecuteInteractiveCommandOptions(ScriptFile scriptFile, string[] arguments, string[] packageSources)
        {
            ScriptFile = scriptFile;
            Arguments = arguments;
            PackageSources = packageSources;
        }

        public ScriptFile ScriptFile { get; }
        public string[] Arguments { get; }
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