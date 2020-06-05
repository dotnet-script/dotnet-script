using System;
using Dotnet.Script.DependencyModel.Environment;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class PublishCommandOptions
    {
        [Obsolete("Use the constructor overload that accepts a " + nameof(CommandScriptCompilationCacheLevel) + " argument instead.")]
        public PublishCommandOptions(ScriptFile file, string outputDirectory, string libraryName, PublishType publishType, OptimizationLevel optimizationLevel, string[] packageSources, string runtimeIdentifier, bool noCache) :
            this(file, outputDirectory, libraryName, publishType, optimizationLevel, packageSources, runtimeIdentifier, noCache.ToCommandScriptCompilationCacheLevel()) {}

        public PublishCommandOptions(ScriptFile file, string outputDirectory, string libraryName, PublishType publishType, OptimizationLevel optimizationLevel, string[] packageSources, string runtimeIdentifier, CommandScriptCompilationCacheLevel cacheLevel)
        {
            File = file;
            OutputDirectory = outputDirectory;
            LibraryName = libraryName;
            PublishType = publishType;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            RuntimeIdentifier = runtimeIdentifier ?? ScriptEnvironment.Default.RuntimeIdentifier;
            CacheLevel = cacheLevel;
        }

        public ScriptFile File { get; }
        public string OutputDirectory { get; }
        public string LibraryName { get; }
        public PublishType PublishType { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public string RuntimeIdentifier { get; }
        public bool NoCache => CacheLevel == CommandScriptCompilationCacheLevel.None;
        public CommandScriptCompilationCacheLevel CacheLevel { get; }
        public CommandScriptCompilationCacheLevel NormalCacheLevel => CacheLevel.Normalize();
    }

    public enum PublishType
    {
        Library,
        Executable
    }
}