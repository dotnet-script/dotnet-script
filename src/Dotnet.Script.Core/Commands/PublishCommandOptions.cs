using Dotnet.Script.DependencyModel.Environment;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class PublishCommandOptions
    {
        public PublishCommandOptions(ScriptFile file, string outputDirectory, string libraryName, PublishType publishType, OptimizationLevel optimizationLevel, string[] packageSources, string runtimeIdentifier, bool noCache, bool noNugetCache)
        {
            File = file;
            OutputDirectory = outputDirectory;
            LibraryName = libraryName;
            PublishType = publishType;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            RuntimeIdentifier = runtimeIdentifier ?? ScriptEnvironment.Default.RuntimeIdentifier;
            NoCache = noCache;
            NoNugetCache = noNugetCache;
        }

        public ScriptFile File { get; }
        public string OutputDirectory { get; }
        public string LibraryName { get; }
        public PublishType PublishType { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public string RuntimeIdentifier { get; }
        public bool NoCache { get; }
        public bool NoNugetCache { get; }
    }

    public enum PublishType
    {
        Library,
        Executable
    }
}