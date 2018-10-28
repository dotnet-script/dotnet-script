using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class FileCommandOptions
    {
        public FileCommandOptions(ScriptFile file, string[] arguments, OptimizationLevel optimizationLevel, string[] packageSources, bool noCache)
        {
            File = file;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            NoCache = noCache;
        }

        public ScriptFile File { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public bool NoCache { get; }
    }
}