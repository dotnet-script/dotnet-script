using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CommandLineUtils;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Scripting;

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
                if (e is AggregateException aggregateEx)
                {
                    e = aggregateEx.Flatten().InnerException;
                }

                if (e is CompilationErrorException || e is ScriptRuntimeException)
                {
                    // no need to write out anything as the upstream services will report that
                    return 0x1;
                }

                // Be verbose (stack trace) in debug mode otherwise brief
                var error = args.Any(arg => arg == DebugFlagShort
                                         || arg == DebugFlagLong)
                          ? e.ToString()
                          : e.GetBaseException().Message;
                Console.Error.WriteLine(error);
                return 0x1;
            }
        }

        private static int Wain(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                ExtendedHelpText = "Starting without a path to a CSX file or a command, starts the REPL (interactive) mode."
            };
            var file = app.Argument("script", "Path to CSX script");            
            var interactive = app.Option("-i |--interactive", "Execute a script and drop into the interactive mode afterwards.", CommandOptionType.NoValue);

            var debugMode = app.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);

            var argsBeforeDoubleHyphen = args.TakeWhile(a => a != "--").ToArray();
            var argsAfterDoubleHypen = args.SkipWhile(a => a != "--").Skip(1).ToArray();

            app.HelpOption("-? | -h | --help");

            app.VersionOption("-v | --version", GetVersionInfo);

            app.Command("eval", c =>
            {
                c.Description = "Execute CSX code.";
                var code = c.Argument("code", "Code to execute.");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);

                c.OnExecute(async () =>
                {
                    int exitCode = 0;
                    if (!string.IsNullOrWhiteSpace(code.Value))
                    {
                        exitCode = await RunCode(code.Value, debugMode.HasValue(), app.RemainingArguments.Concat(argsAfterDoubleHypen), cwd.Value());                        
                    }
                    return exitCode;
                });
            });

            app.Command("init", c =>
            {
                c.Description = "Creates a sample script along with the launch.json file needed to launch and debug the script.";
                c.OnExecute(() =>
                {
                    var scaffolder = new Scaffolder();
                    scaffolder.InitializerFolder();
                    return 0;
                });
            });

            app.Command("new", c =>
            {
                c.Description = "Creates a new script file";
                var fileNameArgument = c.Argument("filename", "The script file name");
                c.OnExecute(() =>
                {
                    var scaffolder = new Scaffolder();
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }
                    scaffolder.CreateNewScriptFile(fileNameArgument.Value);
                    return 0;
                });
            });

            app.OnExecute(async () =>
            {
                int exitCode = 0;
                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    exitCode = await RunScript(file.Value, debugMode.HasValue(), app.RemainingArguments.Concat(argsAfterDoubleHypen), interactive.HasValue());
                }
                else
                {
                    await RunInteractive(debugMode.HasValue());
                }
                return exitCode;
            });

            return app.Execute(argsBeforeDoubleHyphen);            
        }

        private static async Task<int> RunScript(string file, bool debugMode, IEnumerable<string> args, bool interactive)
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
                var context = new ScriptContext(sourceText, directory, args, absoluteFilePath, debugMode);

                if (interactive)
                {
                    var compiler = GetScriptCompiler(debugMode);
                    var runner = new InteractiveRunner(compiler, compiler.Logger, ScriptConsole.Default);
                    await runner.RunLoopWithSeed(debugMode, context);
                    return 0;
                }

                return await Run(debugMode, context);
            }
        }

        private static async Task RunInteractive(bool debugMode)
        {
            var compiler = GetScriptCompiler(debugMode);
            var runner = new InteractiveRunner(compiler, compiler.Logger, ScriptConsole.Default);
            await runner.RunLoop(debugMode);
        }

        private static Task<int> RunCode(string code, bool debugMode, IEnumerable<string> args, string currentWorkingDirectory)
        {
            var sourceText = SourceText.From(code);
            var context = new ScriptContext(sourceText, currentWorkingDirectory ?? Directory.GetCurrentDirectory(), args, null, debugMode);
            return Run(debugMode, context);
        }

        private static Task<int> Run(bool debugMode, ScriptContext context)
        {
            var compiler = GetScriptCompiler(debugMode);
            var runner = new ScriptRunner(compiler, compiler.Logger, ScriptConsole.Default);
            return runner.Execute<int>(context);
        }

        private static string GetVersionInfo()
        {
            var versionAttribute = typeof(Program).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().SingleOrDefault();            
            return versionAttribute?.InformationalVersion;
        }

        private static ScriptCompiler GetScriptCompiler(bool debugMode)
        {
            var logger = new ScriptLogger(ScriptConsole.Default.Error, debugMode);
            var runtimeDependencyResolver = new RuntimeDependencyResolver(type => ((level, message) =>
            {
                if (level == LogLevel.Debug)
                {
                    logger.Verbose(message);
                }
                if (level == LogLevel.Info)
                {
                    logger.Log(message);
                }
            }));

            var compiler = new ScriptCompiler(logger, runtimeDependencyResolver);
            return compiler;
        }
    }
}