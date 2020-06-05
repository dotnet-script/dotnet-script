using System;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteLibraryCommandOptions
    {
        [Obsolete("Use the constructor overload that accepts a " + nameof(CommandScriptCompilationCacheLevel) + " argument instead.")]
        public ExecuteLibraryCommandOptions(string libraryPath, string[] arguments, bool noCache) :
            this(libraryPath, arguments, noCache.ToCommandScriptCompilationCacheLevel()) {}

        public ExecuteLibraryCommandOptions(string libraryPath, string[] arguments, CommandScriptCompilationCacheLevel cacheLevel)
        {
            LibraryPath = libraryPath;
            Arguments = arguments;
            CacheLevel = cacheLevel;
        }

        public string LibraryPath { get; }
        public string[] Arguments { get; }
        public bool NoCache => CacheLevel == CommandScriptCompilationCacheLevel.None;
        public CommandScriptCompilationCacheLevel CacheLevel { get; }
        public CommandScriptCompilationCacheLevel NormalCacheLevel => CacheLevel.Normalize();
    }
}