using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteCodeCommandOptions
    {
        public ExecuteCodeCommandOptions(string code, string workingDirectory, string[] arguments, OptimizationLevel optimizationLevel, bool noCache, string[] packageSources)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            NoCache = noCache;
            PackageSources = packageSources;
        }

        public string Code { get; }
        public string WorkingDirectory { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool NoCache { get; }
        public string[] PackageSources { get; }
    }
}