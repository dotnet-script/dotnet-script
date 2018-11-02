using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteScriptCommandOptions
    {
        public ExecuteScriptCommandOptions(ScriptFile file, string[] arguments, OptimizationLevel optimizationLevel, string[] packageSources, bool isInteractive ,bool noCache)
        {
            File = file;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            IsInteractive = isInteractive;
            NoCache = noCache;
        }

        public ScriptFile File { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public bool IsInteractive { get; }
        public bool NoCache { get; }
    }
}