using System;

namespace Dotnet.Script.DependencyModel.Logging
{
    public delegate Logger LogFactory(Type type);

    public delegate void Logger(LogLevel level, string message);

    public enum LogLevel
    {
        Trace,
        Debug,
        Info      
    }

    public static class LogExtensions
    {
        public static Logger CreateLogger<T>(this LogFactory logFactory) => logFactory(typeof(T));

        public static void Trace(this Logger logger, string message) => logger(LogLevel.Trace, message);
        public static void Debug(this Logger logger, string message) => logger(LogLevel.Debug, message);
        public static void Info(this Logger logger, string message) => logger(LogLevel.Info, message);       
    }
}
