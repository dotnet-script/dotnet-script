using System;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteCodeCommandOptions
    {
        [Obsolete("Use the constructor overload that accepts a " + nameof(CommandScriptCompilationCacheLevel) + " argument instead.")]
        public ExecuteCodeCommandOptions(string code, string workingDirectory, string[] arguments, OptimizationLevel optimizationLevel, bool noCache, string[] packageSources) :
            this(code, workingDirectory, arguments, optimizationLevel, noCache.ToCommandScriptCompilationCacheLevel(), packageSources) {}

        public ExecuteCodeCommandOptions(string code, string workingDirectory, string[] arguments, OptimizationLevel optimizationLevel, CommandScriptCompilationCacheLevel cacheLevel, string[] packageSources)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            CacheLevel = cacheLevel;
            PackageSources = packageSources;
        }

        public string Code { get; }
        public string WorkingDirectory { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool NoCache => CacheLevel == CommandScriptCompilationCacheLevel.None;
        public CommandScriptCompilationCacheLevel CacheLevel { get; }
        public CommandScriptCompilationCacheLevel NormalCacheLevel => CacheLevel.Normalize();
        public string[] PackageSources { get; }
    }
}