using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Console.Internal;

namespace Dotnet.Script
{
    public static class LogHelper
    {
        private static Dictionary<string, LogLevel> _verbosityMap 
            = new Dictionary<string, LogLevel>(StringComparer.InvariantCultureIgnoreCase);

        static LogHelper()
        {
            _verbosityMap.Add("t", LogLevel.Trace);
            _verbosityMap.Add("trace", LogLevel.Trace);
            _verbosityMap.Add("d", LogLevel.Debug);
            _verbosityMap.Add("debug", LogLevel.Debug);
            _verbosityMap.Add("i", LogLevel.Information);
            _verbosityMap.Add("info", LogLevel.Information);
            _verbosityMap.Add("w", LogLevel.Warning);
            _verbosityMap.Add("warning", LogLevel.Warning);
            _verbosityMap.Add("e", LogLevel.Error);
            _verbosityMap.Add("error", LogLevel.Error);
            _verbosityMap.Add("c", LogLevel.Critical);
            _verbosityMap.Add("critical", LogLevel.Critical);            
        }

        private static LogLevel ParseVerbosity(string verbosity)
        {
            if (string.IsNullOrWhiteSpace(verbosity))
            {
                return LogLevel.Warning;
            }

            if (!_verbosityMap.TryGetValue(verbosity, out var logLevel))
            {
                throw new InvalidOperationException($"Unknown verbosity level {verbosity}");
            }

            return logLevel;
        }

        public static DependencyModel.Logging.LogFactory CreateLogFactory(string verbosity, bool debugMode)
        {
            LogLevel logLevel;
            if (debugMode)
            {
                logLevel = LogLevel.Debug;
            }
            else
            {
                logLevel = ParseVerbosity(verbosity);
            }
            
            var loggerFactory = new LoggerFactory();
            
            loggerFactory.AddProvider(new ConsoleErrorLoggerProvider((message, level) => level >= logLevel));

            return type =>
            {
                var logger = loggerFactory.CreateLogger(type);
                return (level, message) =>
                {
                    if (level == DependencyModel.Logging.LogLevel.Trace)
                    {
                        logger.LogTrace(message);
                    }

                    if (level == DependencyModel.Logging.LogLevel.Debug)
                    {
                        logger.LogDebug(message);
                    }

                    if (level == DependencyModel.Logging.LogLevel.Info)
                    {
                        logger.LogInformation(message);
                    }
                };
            };
        }    
    }   

    public class WindowsLogErrorConsole : IConsole
    {
        private void SetColor(ConsoleColor? background, ConsoleColor? foreground)
        {
            if (background.HasValue)
            {
                Console.BackgroundColor = background.Value;
            }

            if (foreground.HasValue)
            {
                Console.ForegroundColor = foreground.Value;
            }
        }

        private void ResetColor()
        {
            Console.ResetColor();
        }

        public void Write(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            SetColor(background, foreground);
            Console.Error.Write(message);
            ResetColor();
        }

        public void WriteLine(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            SetColor(background, foreground);
            Console.Error.WriteLine(message);
            ResetColor();
        }

        public void Flush()
        {
            // No action required as for every write, data is sent directly to the console
            // output stream
        }
    }

    public class AnsiSystemErrorConsole : IAnsiSystemConsole
    {
        public void Write(string message)
        {
            System.Console.Error.Write(message);
        }

        public void WriteLine(string message)
        {
            System.Console.Error.WriteLine(message);
        }
    }

    public class ConsoleErrorLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> _filter;

        public ConsoleErrorLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            _filter = filter;
        }

        public ILogger CreateLogger(string name)
        {
            var consoleLogger = new ConsoleLogger(name, _filter, false);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                consoleLogger.Console = new WindowsLogErrorConsole();
            }
            else
            {
                consoleLogger.Console = new AnsiLogConsole(new AnsiSystemErrorConsole());
            }
            
            return consoleLogger;
        }

        public void Dispose()
        {
        }
    }
}
