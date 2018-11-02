using Dotnet.Script.DependencyModel.Environment;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class PublishCommandOptions
    {
        public PublishCommandOptions(ScriptFile file, string outputDirectory, string libraryName, PublishType publishType, OptimizationLevel optimizationLevel,string runtimeIdentifier, bool noCache)
        {
            File = file;
            OutputDirectory = outputDirectory;
            LibraryName = libraryName;
            PublishType = publishType;
            OptimizationLevel = optimizationLevel;
            RuntimeIdentifier = runtimeIdentifier ?? ScriptEnvironment.Default.RuntimeIdentifier;
            NoCache = noCache;
        }

        public ScriptFile File { get; }
        public string OutputDirectory { get; }
        public string LibraryName { get; }
        public PublishType PublishType { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string RuntimeIdentifier { get; }
        public bool NoCache { get; }
    }

    public enum PublishType
    {
        Library,
        Executable
    }
}