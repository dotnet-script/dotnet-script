using System;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteScriptCommandOptions
    {
        [Obsolete("Use the constructor overload that accepts a " + nameof(CommandScriptCompilationCacheLevel) + " argument instead.")]
        public ExecuteScriptCommandOptions(ScriptFile file, string[] arguments, OptimizationLevel optimizationLevel, string[] packageSources, bool isInteractive, bool noCache) :
            this(file, arguments, optimizationLevel, packageSources, isInteractive, noCache.ToCommandScriptCompilationCacheLevel()) {}

        public ExecuteScriptCommandOptions(ScriptFile file, string[] arguments, OptimizationLevel optimizationLevel, string[] packageSources, bool isInteractive, CommandScriptCompilationCacheLevel cacheLevel)
        {
            File = file;
            Arguments = arguments;
            OptimizationLevel = optimizationLevel;
            PackageSources = packageSources;
            IsInteractive = isInteractive;
            CacheLevel = cacheLevel;
        }

        public ScriptFile File { get; }
        public string[] Arguments { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public string[] PackageSources { get; }
        public bool IsInteractive { get; }
        public bool NoCache => CacheLevel == CommandScriptCompilationCacheLevel.None;
        public CommandScriptCompilationCacheLevel CacheLevel { get; }
        public CommandScriptCompilationCacheLevel NormalCacheLevel => CacheLevel.Normalize();
    }
}