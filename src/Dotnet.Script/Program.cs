using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CommandLineUtils;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel;

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

            app.VersionOption("-v | --version", GetVersionInfo);

            app.Command("eval", c =>
            {
                c.Description = "Execute CSX code.";
                var code = c.Argument("code", "Code to execute.");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);

                c.OnExecute(() =>
                {
                    if (!string.IsNullOrWhiteSpace(code.Value))
                    {
                        RunCode(code.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(), app.RemainingArguments, cwd.Value());
                    }
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    RunScript(file.Value, config.HasValue() ? config.Value() : "Release", debugMode.HasValue(), app.RemainingArguments);
                }
                else
                {
                    app.ShowHelp();
                }
                return 0;
            });

            app.Command("init", c =>
            {
                c.Description = "Creates a sample script along with the launch.json file needed to launch and debug the script.";
                c.OnExecute(() =>
                {
                    var skaffolder = new Scaffolder();
                    skaffolder.InitializerFolder();
                    return 0;
                });
            });

            app.Command("new", c =>
            {
                c.Description = "Creates a new script file";
                var fileNameArgument = c.Argument("filename", "The script file name");
                c.OnExecute(() =>
                {
                    var skaffolder = new Scaffolder();
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }
                    skaffolder.CreateNewScriptFile(fileNameArgument.Value);
                    return 0;
                });
            });

            return app.Execute(args.Except(new[] { "--" }).ToArray());
        }

        private static void RunScript(string file, string config, bool debugMode, IEnumerable<string> args)
        {
            if (!File.Exists(file))
            {
                throw new Exception($"Couldn't find file '{file}'");
            }

            var absoluteFilePath = Path.IsPathRooted(file) ? file : Path.Combine(Directory.GetCurrentDirectory(), file);
            var directory = Path.GetDirectoryName(absoluteFilePath);

            using (var filestream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sourceText = SourceText.From(filestream);
                var context = new ScriptContext(sourceText, directory, config, args, absoluteFilePath, debugMode);
                Run(debugMode, context);
            }
        }

        private static void RunCode(string code, string config, bool debugMode, IEnumerable<string> args, string currentWorkingDirectory)
        {
            var sourceText = SourceText.From(code);
            var context = new ScriptContext(sourceText, currentWorkingDirectory ?? Directory.GetCurrentDirectory(), config, args, null, debugMode);

            Run(debugMode, context);
        }

        private static void Run(bool debugMode, ScriptContext context)
        {
            var logger = new ScriptLogger(Console.Error, debugMode);
            var compiler = new ScriptCompiler(logger, ScriptDependencyResolver.CreateRuntimeResolver(
                (verbose, message) =>
                {
                    if (verbose)
                    {
                        logger.Verbose(message);
                    }
                    else
                    {
                        logger.Log(message);
                    }

                }));
            var runner = new ScriptRunner(compiler, logger);
            runner.Execute<object>(context).GetAwaiter().GetResult();
        }

        private static string GetVersionInfo()
        {
            var versionAttribute = typeof(Program).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().SingleOrDefault();            
            return versionAttribute?.InformationalVersion;
        }
    }

}