using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Dotnet.Script
{
    public static class LogHelper
    {
        public static LogFactory CreateLogFactory(string verbosity)
        {
            var logLevel = (LogLevel)LevelMapper.FromString(verbosity);

            var loggerFilterOptions = new LoggerFilterOptions() { MinLevel = logLevel };

            var consoleLoggerProvider = new ConsoleLoggerProvider(new ConsoleOptionsMonitor());

            var loggerFactory = new LoggerFactory(new[] { consoleLoggerProvider }, loggerFilterOptions);

            return type =>
            {
                var logger = loggerFactory.CreateLogger(type);
                return (level, message, exception) =>
                {
                    logger.Log((LogLevel)level, message, exception);
                };
            };
        }
    }

    internal class ConsoleOptionsMonitor : IOptionsMonitor<ConsoleLoggerOptions>
    {
        private ConsoleLoggerOptions _consoleLoggerOptions;

        public ConsoleOptionsMonitor()
        {
            _consoleLoggerOptions = new ConsoleLoggerOptions()
            {
                LogToStandardErrorThreshold = LogLevel.Trace
            };
        }

        public ConsoleLoggerOptions CurrentValue => _consoleLoggerOptions;

        public ConsoleLoggerOptions Get(string name) => _consoleLoggerOptions;

        public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener)
        {
            return null;
        }
    }
}
