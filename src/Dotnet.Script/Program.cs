using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Scripting;
using Dotnet.Script.DependencyModel.Context;
using Microsoft.CodeAnalysis;
using System.Text;
using Dotnet.Script.DependencyModel.Environment;
using McMaster.Extensions.CommandLineUtils;

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
            var interactive = app.Option("-i | --interactive", "Execute a script and drop into the interactive mode afterwards.", CommandOptionType.NoValue);

            var configuration = app.Option("-c | --configuration <configuration>", "Configuration to use for running the script [Release/Debug] Default is \"Debug\"", CommandOptionType.SingleValue);
            
            var debugMode = app.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);

            var argsBeforeDoubleHyphen = args.TakeWhile(a => a != "--").ToArray();
            var argsAfterDoubleHypen = args.SkipWhile(a => a != "--").Skip(1).ToArray();

            app.HelpOption("-? | -h | --help");

            app.VersionOption("-v | --version", GetVersion);

            var infoOption = app.Option("--info", "Displays environmental information", CommandOptionType.NoValue);

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
                        var optimizationLevel = OptimizationLevel.Debug;
                        if (configuration.HasValue() && configuration.Value().ToLower() == "release")
                        {
                            optimizationLevel = OptimizationLevel.Release;
                        }
                        exitCode = await RunCode(code.Value, debugMode.HasValue(), optimizationLevel, app.RemainingArguments.Concat(argsAfterDoubleHypen), cwd.Value());                        
                    }
                    return exitCode;
                });
            });

            app.Command("init", c =>
            {
                c.Description = "Creates a sample script along with the launch.json file needed to launch and debug the script.";
                var fileName = c.Argument("filename", "(Optional) The name of the sample script file to be created during initialization. Defaults to 'main.csx'");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for initialization. Defaults to current directory.", CommandOptionType.SingleValue);
                c.OnExecute(() =>
                {
                    var scaffolder = new Scaffolder(new ScriptLogger(ScriptConsole.Default.Error, debugMode.HasValue()));
                    scaffolder.InitializerFolder(fileName.Value, cwd.Value() ?? Directory.GetCurrentDirectory());
                    return 0;
                });
            });

            app.Command("new", c =>
            {
                c.Description = "Creates a new script file";
                var fileNameArgument = c.Argument("filename", "The script file name");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory the new script file to be created. Defaults to current directory.", CommandOptionType.SingleValue);
                c.OnExecute(() =>
                {
                    var scaffolder = new Scaffolder(new ScriptLogger(ScriptConsole.Default.Error, debugMode.HasValue()));
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }
                    scaffolder.CreateNewScriptFile(fileNameArgument.Value, cwd.Value() ?? Directory.GetCurrentDirectory());
                    return 0;
                });
            });

            app.OnExecute(async () =>
            {
                int exitCode = 0;

                if (infoOption.HasValue())
                {
                    Console.Write(GetEnvironmentInfo());
                    return 0;
                }

                if (!string.IsNullOrWhiteSpace(file.Value))
                {
                    var optimizationLevel = OptimizationLevel.Debug;
                    if (configuration.HasValue() && configuration.Value().ToLower() == "release")
                    {
                        optimizationLevel = OptimizationLevel.Release;
                    }
                    exitCode = await RunScript(file.Value, debugMode.HasValue(), optimizationLevel, app.RemainingArguments.Concat(argsAfterDoubleHypen), interactive.HasValue());
                }
                else
                {
                    await RunInteractive(debugMode.HasValue());
                }
                return exitCode;
            });

            return app.Execute(argsBeforeDoubleHyphen);            
        }

        private static async Task<int> RunScript(string file, bool debugMode, OptimizationLevel optimizationLevel,  IEnumerable<string> args, bool interactive)
        {
            if (!File.Exists(file))
            {
                if (IsHttpUri(file))
                {
                    var downloader = new ScriptDownloader();
                    var code = await downloader.Download(file);
                    return await RunCode(code, debugMode, optimizationLevel, args, Directory.GetCurrentDirectory());                    
                }

                throw new Exception($"Couldn't find file '{file}'");
            }

            var absoluteFilePath = Path.IsPathRooted(file) ? file : Path.Combine(Directory.GetCurrentDirectory(), file);
            var directory = Path.GetDirectoryName(absoluteFilePath);

            using (var filestream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sourceText = SourceText.From(filestream);
                var context = new ScriptContext(sourceText, directory, args, absoluteFilePath, optimizationLevel);

                if (interactive)
                {
                    var compiler = GetScriptCompiler(debugMode);
                    var runner = new InteractiveRunner(compiler, compiler.Logger, ScriptConsole.Default);
                    await runner.RunLoopWithSeed(context);
                    return 0;
                }

                return await Run(debugMode, context);
            }
        }

        private static bool IsHttpUri(string fileName)
        {
            return Uri.TryCreate(fileName, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);           
        }

        private static async Task RunInteractive(bool debugMode)
        {
            var compiler = GetScriptCompiler(debugMode);
            var runner = new InteractiveRunner(compiler, compiler.Logger, ScriptConsole.Default);
            await runner.RunLoop();
        }

        private static Task<int> RunCode(string code, bool debugMode, OptimizationLevel optimizationLevel, IEnumerable<string> args, string currentWorkingDirectory)
        {
            var sourceText = SourceText.From(code);
            var context = new ScriptContext(sourceText, currentWorkingDirectory ?? Directory.GetCurrentDirectory(), args, null,optimizationLevel, ScriptMode.Eval);
            return Run(debugMode, context);
        }

        private static Task<int> Run(bool debugMode, ScriptContext context)
        {
            var compiler = GetScriptCompiler(debugMode);
            var runner = new ScriptRunner(compiler, compiler.Logger, ScriptConsole.Default);
            return runner.Execute<int>(context);
        }

        private static string GetEnvironmentInfo()
        {
            var netCoreVersion = ScriptEnvironment.Default.NetCoreVersion;
            StringBuilder sb = new StringBuilder();            
            sb.AppendLine($"Version             : {GetVersion()}");            
            sb.AppendLine($"Install location    : {ScriptEnvironment.Default.InstallLocation}");
            sb.AppendLine($"Target framework    : {netCoreVersion.Tfm}");
            sb.AppendLine($".NET Core version   : {netCoreVersion.Version}");
            sb.AppendLine($"Platform identifier : {ScriptEnvironment.Default.PlatformIdentifier}");
            sb.AppendLine($"Runtime identifier  : {ScriptEnvironment.Default.RuntimeIdentifier}");
            return sb.ToString();            
        }

        private static string GetVersion()
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