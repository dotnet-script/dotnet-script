using Dotnet.Script.Core;
using Dotnet.Script.Core.Commands;
using Dotnet.Script.Core.Versioning;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dotnet.Script
{
    public static class Program
    {
        private const string DebugFlagShort = "-d";
        private const string DebugFlagLong = "--debug";

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

        public static Func<string, bool, LogFactory> CreateLogFactory
            = (verbosity, debugMode) => LogHelper.CreateLogFactory(debugMode ? "debug" : verbosity);

        private static int Wain(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                ExtendedHelpText = "Starting without a path to a CSX file or a command, starts the REPL (interactive) mode."
            };

            var file = app.Argument("script", "Path to CSX script");
            var interactive = app.Option("-i | --interactive", "Execute a script and drop into the interactive mode afterwards.", CommandOptionType.NoValue);
            var configuration = app.Option("-c | --configuration <configuration>", "Configuration to use for running the script [Release/Debug] Default is \"Debug\"", CommandOptionType.SingleValue);
            var packageSources = app.Option("-s | --sources <SOURCE>", "Specifies a NuGet package source to use when resolving NuGet packages.", CommandOptionType.MultipleValue);
            var debugMode = app.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);
            var verbosity = app.Option("--verbosity", " Set the verbosity level of the command. Allowed values are t[trace], d[ebug], i[nfo], w[arning], e[rror], and c[ritical].", CommandOptionType.SingleValue);
            var nocache = app.Option("--no-cache", "Disable caching (Restore and Dll cache)", CommandOptionType.NoValue);
            var infoOption = app.Option("--info", "Displays environmental information", CommandOptionType.NoValue);
            var sdk = app.Option("--sdk <sdk>", "Project SDK to use. Default is \"Microsoft.NET.Sdk\"", CommandOptionType.SingleValue);

            var argsBeforeDoubleHyphen = args.TakeWhile(a => a != "--").ToArray();
            var argsAfterDoubleHyphen  = args.SkipWhile(a => a != "--").Skip(1).ToArray();

            const string helpOptionTemplate = "-? | -h | --help";
            app.HelpOption(helpOptionTemplate);
            app.VersionOption("-v | --version", () => new VersionProvider().GetCurrentVersion().Version);

            app.Command("eval", c =>
            {
                c.Description = "Execute CSX code.";
                var code = c.Argument("code", "Code to execute.");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);
                c.HelpOption(helpOptionTemplate);
                c.OnExecute(async () =>
                {
                    var source = code.Value;
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        if (Console.IsInputRedirected)
                        {
                            source = await Console.In.ReadToEndAsync();
                        }
                        else
                        {
                            c.ShowHelp();
                            return 0;
                        }
                    }

                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    var options = new ExecuteCodeCommandOptions(source, cwd.Value(), app.RemainingArguments.Concat(argsAfterDoubleHyphen).ToArray(),configuration.ValueEquals("release", StringComparison.OrdinalIgnoreCase) ? OptimizationLevel.Release : OptimizationLevel.Debug, nocache.HasValue(),packageSources.Values?.ToArray());
                    return await new ExecuteCodeCommand(ScriptConsole.Default, logFactory).Execute<int>(options);
                });
            });

            app.Command("init", c =>
            {
                c.Description = "Creates a sample script along with the launch.json file needed to launch and debug the script.";
                var fileName = c.Argument("filename", "(Optional) The name of the sample script file to be created during initialization. Defaults to 'main.csx'");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for initialization. Defaults to current directory.", CommandOptionType.SingleValue);
                c.HelpOption(helpOptionTemplate);
                c.OnExecute(() =>
                {
                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    new InitCommand(logFactory).Execute(new InitCommandOptions(fileName.Value, cwd.Value()));
                    return 0;
                });
            });

            app.Command("new", c =>
            {
                c.Description = "Creates a new script file";
                var fileNameArgument = c.Argument("filename", "The script file name");
                var cwd = c.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory the new script file to be created. Defaults to current directory.", CommandOptionType.SingleValue);
                c.HelpOption(helpOptionTemplate);
                c.OnExecute(() =>
                {
                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    var scaffolder = new Scaffolder(logFactory);
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }
                    scaffolder.CreateNewScriptFile(fileNameArgument.Value, cwd.Value() ?? Directory.GetCurrentDirectory());
                    return 0;
                });
            });

            // windows only 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // on windows we have command to register .csx files to be executed by dotnet-script
                app.Command("register", c =>
                {
                    c.Description = "Register .csx file handler to enable running scripts directly";
                    c.HelpOption(helpOptionTemplate);
                    c.OnExecute(() =>
                    {
                        var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                        var scaffolder = new Scaffolder(logFactory);
                        scaffolder.RegisterFileHandler();
                    });
                });
            }

            app.Command("publish", c =>
            {
                c.Description = "Creates a self contained executable or DLL from a script";
                var fileNameArgument = c.Argument("filename", "The script file name");
                var publishDirectoryOption = c.Option("-o |--output", "Directory where the published executable should be placed.  Defaults to a 'publish' folder in the current directory.", CommandOptionType.SingleValue);
                var dllName = c.Option("-n |--name", "The name for the generated DLL (executable not supported at this time).  Defaults to the name of the script.", CommandOptionType.SingleValue);
                var dllOption = c.Option("--dll", "Publish to a .dll instead of an executable.", CommandOptionType.NoValue);
                var commandConfig = c.Option("-c | --configuration <configuration>", "Configuration to use for publishing the script [Release/Debug]. Default is \"Debug\"", CommandOptionType.SingleValue);
                var runtime = c.Option("-r |--runtime", "The runtime used when publishing the self contained executable. Defaults to your current runtime.", CommandOptionType.SingleValue);
                c.HelpOption(helpOptionTemplate);
                c.OnExecute(() =>
                {
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }

                    var options = new PublishCommandOptions
                    (
                        new ScriptFile(fileNameArgument.Value),
                        publishDirectoryOption.Value(),
                        dllName.Value(),
                        dllOption.HasValue() ? PublishType.Library : PublishType.Executable,
                        commandConfig.ValueEquals("release", StringComparison.OrdinalIgnoreCase) ? OptimizationLevel.Release : OptimizationLevel.Debug,
                        packageSources.Values?.ToArray(),
                        runtime.Value() ?? ScriptEnvironment.Default.RuntimeIdentifier,
                        nocache.HasValue(),
                        sdk.Value()
                    );

                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    new PublishCommand(ScriptConsole.Default, logFactory).Execute(options);
                    return 0;
                });
            });

            app.Command("exec", c =>
            {
                c.Description = "Run a script from a DLL.";
                var dllPath = c.Argument("dll", "Path to DLL based script");
                var commandDebugMode = c.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);
                c.HelpOption(helpOptionTemplate);
                c.OnExecute(async () =>
                {
                    if (string.IsNullOrWhiteSpace(dllPath.Value))
                    {
                        c.ShowHelp();
                        return 0;
                    }

                    var options = new ExecuteLibraryCommandOptions
                    (
                        dllPath.Value,
                        app.RemainingArguments.Concat(argsAfterDoubleHyphen).ToArray(),
                        nocache.HasValue()
                    );
                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    return await new ExecuteLibraryCommand(ScriptConsole.Default, logFactory).Execute<int>(options);
                });
            });

            app.OnExecute(async () =>
            {
                int exitCode = 0;

                var scriptFile = new ScriptFile(file.Value);
                var optimizationLevel = configuration.ValueEquals("release", StringComparison.OrdinalIgnoreCase) ? OptimizationLevel.Release : OptimizationLevel.Debug;
                var scriptArguments = app.RemainingArguments.Concat(argsAfterDoubleHyphen).ToArray();
                var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                if (infoOption.HasValue())
                {
                    var environmentReporter = new EnvironmentReporter(logFactory);
                    await environmentReporter.ReportInfo();
                    return 0;
                }

                if (scriptFile.HasValue)
                {
                    if (interactive.HasValue())
                    {
                        return await RunInteractiveWithSeed(file.Value, logFactory, scriptArguments, packageSources.Values?.ToArray());
                    }

                    var fileCommandOptions = new ExecuteScriptCommandOptions
                    (
                        new ScriptFile(file.Value),
                        scriptArguments,
                        optimizationLevel,
                        packageSources.Values?.ToArray(),
                        interactive.HasValue(),
                        nocache.HasValue(),
                        sdk.Value()
                    );

                    var fileCommand = new ExecuteScriptCommand(ScriptConsole.Default, logFactory);
                    return await fileCommand.Run<int, CommandLineScriptGlobals>(fileCommandOptions);
            }
                else
                {
                    await RunInteractive(!nocache.HasValue(), logFactory, packageSources.Values?.ToArray());
                }
                return exitCode;
            });

            return app.Execute(argsBeforeDoubleHyphen);
        }

        private static async Task<int> RunInteractive(bool useRestoreCache, LogFactory logFactory, string[] packageSources)
        {
            var options = new ExecuteInteractiveCommandOptions(null, Array.Empty<string>(), packageSources);
            await new ExecuteInteractiveCommand(ScriptConsole.Default, logFactory).Execute(options);
            return 0;
        }

        private async static Task<int> RunInteractiveWithSeed(string file, LogFactory logFactory, string[] arguments, string[] packageSources)
        {
            var options = new ExecuteInteractiveCommandOptions(new ScriptFile(file), arguments, packageSources);
            await new ExecuteInteractiveCommand(ScriptConsole.Default, logFactory).Execute(options);
            return 0;
        }
    }
}
