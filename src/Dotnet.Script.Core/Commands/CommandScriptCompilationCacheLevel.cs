namespace Dotnet.Script.Core.Commands
{
    public enum CommandScriptCompilationCacheLevel
    {
        Default,
        None,
        Simple,
        Aggressive,
    }

    public static class CommandScriptCompilationCacheLevelDefault
    {
        public static CommandScriptCompilationCacheLevel Value = CommandScriptCompilationCacheLevel.Aggressive;

        public static CommandScriptCompilationCacheLevel Normalize(this CommandScriptCompilationCacheLevel level) =>
            level == CommandScriptCompilationCacheLevel.Default ? Value : level;
    }

    internal static class CommandScriptCompilationCacheLevelConverter
    {
        public static CommandScriptCompilationCacheLevel ToCommandScriptCompilationCacheLevel(this bool value) =>
            value ? CommandScriptCompilationCacheLevel.None : CommandScriptCompilationCacheLevel.Default;
    }
}
