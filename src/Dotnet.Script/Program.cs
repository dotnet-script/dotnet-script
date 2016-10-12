using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace Dotnet.Script
{
    public class Program
    {
        const string DebugFlagShort = "-d";
        const string DebugFlagLong = "--debug";

        public static int Main(string[] args)
        {
            try
            {
                return Wain(args);
            }
            catch (Exception e)
            {
                // Be verbose (stack trace) in debug mode otherwise brief
                var error = args.Any(arg => arg == DebugFlagShort
                                         || arg == DebugFlagLong)
                          ? e.ToString()
                          : e.GetBaseException().Message;
                Console.Error.WriteLine(error);
                return 0xbad;
            }
        }

        private static int Wain(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            var file = commandLineApplication.Argument("script", "Path to CSX script");

            var scriptArgs = commandLineApplication.Option("-a |--arg <args>", "Arguments to pass to a script. Multiple values supported", CommandOptionType.MultipleValue);
            var config = commandLineApplication.Option("-c |--configuration <configuration>", "Configuration to use. Defaults to 'Release'", CommandOptionType.SingleValue);
            var debugMode = commandLineApplication.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    RunScript(file.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(), scriptArgs.HasValue() ? scriptArgs.Values : new List<string>());
                }
                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static void RunScript(string file, string config, bool debugMode, List<string> scriptArgs)
        {
            var context = new ScriptContext(file, config, scriptArgs);

            var runner = debugMode ? new DebugScriptRunner(Console.Error) : new ScriptRunner(Console.Error);
            runner.Execute<object>(context).GetAwaiter().GetResult();
        }
    }

}