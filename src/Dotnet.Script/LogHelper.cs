using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Console.Internal;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Dotnet.Script
{
    public static class LogHelper
    {                     
        public static LogFactory CreateLogFactory(string verbosity)
        {
            var logLevel = (LogLevel)LevelMapper.FromString(verbosity);
            
            var loggerFactory = new LoggerFactory();            

            loggerFactory.AddProvider(new ConsoleErrorLoggerProvider((message, level) => level >= logLevel));                        

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

        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new ConcurrentDictionary<string, ConsoleLogger>();

        private readonly ConsoleLoggerProcessor _messageQueue = new ConsoleLoggerProcessor();

        private readonly static ConstructorInfo ConsoleLoggerConstructor = typeof(ConsoleLogger).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public ConsoleErrorLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            _filter = filter;
        }
    
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);            
        }

        private ConsoleLogger CreateLoggerImplementation(string name)
        {            
            var consoleLogger = (ConsoleLogger)ConsoleLoggerConstructor.Invoke(new object[] { name, _filter, null, _messageQueue });
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
            _messageQueue.Dispose();
        }
    }   
}
