using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dotnet.Script.Core;
using Microsoft.CodeAnalysis.Text;
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
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            var file = app.Argument("script", "Path to CSX script");
            var config = app.Option("-conf |--configuration <configuration>", "Configuration to use. Defaults to 'Release'", CommandOptionType.SingleValue);
            var debugMode = app.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);

            app.HelpOption("-? | -h | --help");

            var scriptArgsMarkerIndex = Array.FindIndex(args, arg => arg == "--");
            var argCount = scriptArgsMarkerIndex >= 0 ? scriptArgsMarkerIndex : args.Length;
            var scriptArgs = args.Skip(argCount + 1);

            app.Command("eval", c =>
            {
                c.Description = "Execute CSX code.";
                var code = c.Argument("code", "Code to execute.");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);

                c.OnExecute(() =>
                {
                    if (!string.IsNullOrWhiteSpace(code.Value))
                    {
                        RunCode(code.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(), scriptArgs, cwd.Value());
                    }
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    RunScript(file.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(),
                        scriptArgs);
                }
                else
                {
                    app.ShowHelp();
                }
                return 0;
            });

            return app.Execute(args.Take(argCount).ToArray());
        }

        private static void RunScript(string file, string config, bool debugMode, IEnumerable<string> args)
        {
            if (!File.Exists(file))
            {
                throw new Exception($"Couldn't find file '{file}'");
            }

            var directory = Path.IsPathRooted(file) ? Path.GetDirectoryName(file) : Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), file));
            var sourceText = SourceText.From(new FileStream(file, FileMode.Open), Encoding.UTF8);
            var context = new ScriptContext(sourceText, directory, config, args, file);

            Run(debugMode, context);
        }

        private static void RunCode(string code, string config, bool debugMode, IEnumerable<string> args, string currentWorkingDirectory)
        {
            var sourceText = SourceText.From(code, Encoding.UTF8);
            var context = new ScriptContext(sourceText, currentWorkingDirectory ?? Directory.GetCurrentDirectory(), config, args);

            Run(debugMode, context);
        }

        private static void Run(bool debugMode, ScriptContext context)
        {
            var logger = new ScriptLogger(Console.Error, debugMode);
            var compiler = new ScriptCompiler(logger);
            var runner = debugMode ? new DebugScriptRunner(compiler, logger) : new ScriptRunner(compiler, logger);
            runner.Execute<object>(context).GetAwaiter().GetResult();
        }
    }

}