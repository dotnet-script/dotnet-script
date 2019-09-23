using System;
using System.Collections.Generic;

namespace Dotnet.Script.DependencyModel.Logging
{
    public delegate Logger LogFactory(Type type);

    public delegate void Logger(LogLevel level, string message, Exception ex = null);
    
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class LogExtensions
    {
        public static Logger CreateLogger<T>(this LogFactory logFactory) => logFactory(typeof(T));

        public static void Trace(this Logger logger, string message) => logger(LogLevel.Trace, message);
        public static void Debug(this Logger logger, string message) => logger(LogLevel.Debug, message);
        public static void Info(this Logger logger, string message) => logger(LogLevel.Info, message);
        public static void Warning(this Logger logger, string message) => logger(LogLevel.Warning, message);
        public static void Error(this Logger logger, string message, Exception exception = null) => logger(LogLevel.Error, message, exception);
        public static void Critical(this Logger logger, string message, Exception exception = null) => logger(LogLevel.Critical, message, exception);
    }

    public static class LevelMapper
    {
        private static Dictionary<string, LogLevel> _levelMap = CreateMap();

        private static Dictionary<string, LogLevel> CreateMap()
        {
            var map = new Dictionary<string, LogLevel>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "t", LogLevel.Trace },
                { "trace", LogLevel.Trace },
                { "d", LogLevel.Debug },
                { "debug", LogLevel.Debug },
                { "i", LogLevel.Info },
                { "info", LogLevel.Info },
                { "w", LogLevel.Warning },
                { "warning", LogLevel.Warning },
                { "e", LogLevel.Error },
                { "error", LogLevel.Error },
                { "c", LogLevel.Critical },
                { "critical", LogLevel.Critical }
            };
            return map;
        }

        public static LogLevel FromString(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName) || !_levelMap.TryGetValue(levelName, out var level))
            {
                return LogLevel.Warning;
            }
            
            return level;
        }
    }
}
