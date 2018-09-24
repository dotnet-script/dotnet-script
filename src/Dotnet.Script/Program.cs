#define AUTOPUBLISH
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

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

            CommandOption register = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                register = app.Option("--register", "Register .csx file extension to be executable by dotnet-script.", CommandOptionType.NoValue);
            }

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
                        var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                        exitCode = await RunCode(code.Value, debugMode.HasValue(), logFactory, optimizationLevel, app.RemainingArguments.Concat(argsAfterDoubleHypen), cwd.Value(), packageSources.Values?.ToArray());
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
                    var scaffolder = new Scaffolder();
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
                    var scaffolder = new Scaffolder();
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }
                    scaffolder.CreateNewScriptFile(fileNameArgument.Value, cwd.Value() ?? Directory.GetCurrentDirectory());
                    return 0;
                });
            });

            app.Command("publish", c =>
            {
                c.Description = "Creates a self contained executable or DLL from a script";
                var fileNameArgument = c.Argument("filename", "The script file name");
                var publishDirectoryOption = c.Option("-o |--output", "Directory where the published executable should be placed.  Defaults to a 'publish' folder in the current directory.", CommandOptionType.SingleValue);
                var dllName = c.Option("-n |--name", "The name for the generated DLL (executable not supported at this time).  Defaults to the name of the script.", CommandOptionType.SingleValue);
                var dllOption = c.Option("--dll", "Publish to a .dll instead of an executable.", CommandOptionType.NoValue);
                var commandConfig = c.Option("-c | --configuration <configuration>", "Configuration to use for publishing the script [Release/Debug]. Default is \"Debug\"", CommandOptionType.SingleValue);
                var publishDebugMode = c.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);
                var runtime = c.Option("-r |--runtime", "The runtime used when publishing the self contained executable. Defaults to your current runtime.", CommandOptionType.SingleValue);

                c.OnExecute(() =>
                {
                    if (fileNameArgument.Value == null)
                    {
                        c.ShowHelp();
                        return 0;
                    }

                    var optimizationLevel = OptimizationLevel.Debug;
                    if (commandConfig.HasValue() && commandConfig.Value().ToLower() == "release")
                    {
                        optimizationLevel = OptimizationLevel.Release;
                    }

                    var runtimeIdentifier = runtime.Value() ?? ScriptEnvironment.Default.RuntimeIdentifier;
                    var absoluteFilePath = Path.IsPathRooted(fileNameArgument.Value) ? fileNameArgument.Value : Path.Combine(Directory.GetCurrentDirectory(), fileNameArgument.Value);

                    // if a publish directory has been specified, then it is used directly, otherwise:
                    // -- for EXE {current dir}/publish/{runtime ID}
                    // -- for DLL {current dir}/publish
                    var publishDirectory = publishDirectoryOption.Value() ??
                        (dllOption.HasValue() ? Path.Combine(Path.GetDirectoryName(absoluteFilePath), "publish") : Path.Combine(Path.GetDirectoryName(absoluteFilePath), "publish", runtimeIdentifier));

                    var absolutePublishDirectory = Path.IsPathRooted(publishDirectory) ? publishDirectory : Path.Combine(Directory.GetCurrentDirectory(), publishDirectory);

                    var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());
                    var compiler = GetScriptCompiler(publishDebugMode.HasValue(), logFactory);
                    var scriptEmmiter = new ScriptEmitter(ScriptConsole.Default, compiler);
                    var publisher = new ScriptPublisher(logFactory, scriptEmmiter);
                    var code = SourceText.From(File.ReadAllText(absoluteFilePath));
                    var context = new ScriptContext(code, absolutePublishDirectory, Enumerable.Empty<string>(), absoluteFilePath, optimizationLevel);

                    if (dllOption.HasValue())
                    {
                        publisher.CreateAssembly<int, CommandLineScriptGlobals>(context, logFactory, dllName.Value());
                    }
                    else
                    {
                        publisher.CreateExecutable<int, CommandLineScriptGlobals>(context, logFactory, runtimeIdentifier);
                    }

                    return 0;
                });
            });

            app.Command("exec", c =>
            {
                c.Description = "Run a script from a DLL.";
                var dllPath = c.Argument("dll", "Path to DLL based script");
                var commandDebugMode = c.Option(DebugFlagShort + " | " + DebugFlagLong, "Enables debug output.", CommandOptionType.NoValue);
                c.OnExecute(async () =>
                {
                    int exitCode = 0;
                    if (!string.IsNullOrWhiteSpace(dllPath.Value))
                    {
                        if (!File.Exists(dllPath.Value))
                        {
                            throw new Exception($"Couldn't find file '{dllPath.Value}'");
                        }

                        var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());

                        var absoluteFilePath = Path.IsPathRooted(dllPath.Value) ? dllPath.Value : Path.Combine(Directory.GetCurrentDirectory(), dllPath.Value);

                        var compiler = GetScriptCompiler(commandDebugMode.HasValue(), logFactory);
                        var runner = new ScriptRunner(compiler, logFactory, ScriptConsole.Default);
                        var result = await runner.Execute<int>(absoluteFilePath, app.RemainingArguments.Concat(argsAfterDoubleHypen));
                        return result;
                    }
                    return exitCode;
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

                if (register != null && register.HasValue())
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        string cmdPath = Path.Combine(Path.GetTempPath(), "register.cmd");
                        using (var textWriter = new StreamWriter(cmdPath))
                        {
                            textWriter.WriteLine("reg add HKCU\\Software\\classes\\.csx /f /ve /t REG_SZ -d dotnetscript");
                            textWriter.WriteLine("reg add HKCU\\Software\\Classes\\dotnetscript\\Shell\\Open\\Command /f /ve /t REG_EXPAND_SZ /d \"%%USERPROFILE%%\\.dotnet\\tools\\dotnet-script.exe %%1 -- %%*");
                        }
                        Process.Start("cmd.exe", $"/c {cmdPath}").WaitForExit();
                        Console.WriteLine("You can now execute .csx files directly.");
                        return 0;
                    }
                }

                var logFactory = CreateLogFactory(verbosity.Value(), debugMode.HasValue());

                if (!string.IsNullOrWhiteSpace(file.Value))
                {
#if AUTOPUBLISH
                    var absoluteSourcePath = Path.IsPathRooted(file.Value) ? file.Value : Path.Combine(Directory.GetCurrentDirectory(), file.Value);
                    // create unique folder name based on the path
                    string uniqueFolderName = "";
                    using (var sha = SHA256.Create())
                    {
                        uniqueFolderName = Convert.ToBase64String(sha.ComputeHash(Encoding.Unicode.GetBytes(absoluteSourcePath))).Replace("=",String.Empty).Replace("/",string.Empty);
                    }

                    // given the path to a script we create a %temp%\dotnet-scripts\{uniqueFolderName} path
                    string publishDirectory = Path.Combine(Path.GetTempPath(), $"dotnet-scripts\\{uniqueFolderName}");
                    string pathToDll = Path.Combine(publishDirectory, Path.GetFileNameWithoutExtension(absoluteSourcePath) + ".dll");

                    // compute a hash on the contents of a script
                    string sourceHash;
                    using (var sha256 = SHA256.Create())
                    {
                        sourceHash = Convert.ToBase64String(sha256.ComputeHash(File.ReadAllBytes(absoluteSourcePath)));
                    }

                    // get hash code from previous run 
                    string hashFile = Path.Combine(publishDirectory, ".hash");
                    var compiler = GetScriptCompiler(true, logFactory);
                    
                    // if we don't have hash
                    if (!File.Exists(hashFile) ||
                         // or we haven't created a dll
                         !Directory.Exists(publishDirectory) ||
                         // the hashcode has changed (meaning new content)
                         File.ReadAllText(hashFile) != sourceHash ||
                         // or if the dll is older then the file.
                         File.GetCreationTimeUtc(pathToDll) < File.GetCreationTimeUtc(absoluteSourcePath))
                    {
                        // then we autopublish into the %temp%\dotnet-scripts\{uniqueFolderName} path
                        var optimizationLevel = OptimizationLevel.Debug;
                        if (configuration.HasValue() && configuration.Value().ToLower() == "release")
                        {
                            optimizationLevel = OptimizationLevel.Release;
                        }

                        var runtimeIdentifier = ScriptEnvironment.Default.RuntimeIdentifier;
                        var scriptEmmiter = new ScriptEmitter(ScriptConsole.Default, compiler);
                        var publisher = new ScriptPublisher(logFactory, scriptEmmiter);
                        var code = SourceText.From(File.ReadAllText(absoluteSourcePath));
                        var context = new ScriptContext(code, publishDirectory, Enumerable.Empty<string>(), absoluteSourcePath, optimizationLevel);

                        publisher.CreateAssembly<int, CommandLineScriptGlobals>(context, logFactory, Path.GetFileNameWithoutExtension(pathToDll));

                        // save sourceHash for next time, so we can know it's ok to use the generated dll next time
                        File.WriteAllText(hashFile, sourceHash);
                    }

                    // run the %temp%\dotnet-scripts\{uniqueFolderName}/package.dll
                    var runner = new ScriptRunner(compiler, logFactory, ScriptConsole.Default);
                    var result = await runner.Execute<int>(pathToDll, app.RemainingArguments.Concat(argsAfterDoubleHypen));
                    return result;
#else
                    exitCode = await RunScript(file.Value, debugMode.HasValue(), logFactory, optimizationLevel, app.RemainingArguments.Concat(argsAfterDoubleHypen), interactive.HasValue(), packageSources.Values?.ToArray());
#endif
                }
                else
                {
                    await RunInteractive(debugMode.HasValue(), logFactory, packageSources.Values?.ToArray());
                }
                return exitCode;
            });


            return app.Execute(argsBeforeDoubleHyphen);
        }

        private static async Task<int> RunScript(string file, bool debugMode, LogFactory logFactory, OptimizationLevel optimizationLevel, IEnumerable<string> args, bool interactive, string[] packageSources)
        {
            if (!File.Exists(file))
            {
                if (IsHttpUri(file))
                {
                    var downloader = new ScriptDownloader();
                    var code = await downloader.Download(file);
                    return await RunCode(code, debugMode, logFactory, optimizationLevel, args, Directory.GetCurrentDirectory(), packageSources);
                }

                throw new Exception($"Couldn't find file '{file}'");
            }

            var absoluteFilePath = Path.IsPathRooted(file) ? file : Path.Combine(Directory.GetCurrentDirectory(), file);
            var directory = Path.GetDirectoryName(absoluteFilePath);

            using (var filestream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sourceText = SourceText.From(filestream);
                var context = new ScriptContext(sourceText, directory, args, absoluteFilePath, optimizationLevel, packageSources: packageSources);
                if (interactive)
                {
                    var compiler = GetScriptCompiler(debugMode, logFactory);

                    var runner = new InteractiveRunner(compiler, logFactory, ScriptConsole.Default, packageSources);
                    await runner.RunLoopWithSeed(context);
                    return 0;
                }

                return await Run(debugMode, logFactory, context);
            }
        }

        private static bool IsHttpUri(string fileName)
        {
            return Uri.TryCreate(fileName, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static async Task RunInteractive(bool debugMode, LogFactory logFactory, string[] packageSources)
        {
            var compiler = GetScriptCompiler(debugMode, logFactory);
            var runner = new InteractiveRunner(compiler, logFactory, ScriptConsole.Default, packageSources);
            await runner.RunLoop();
        }

        private static Task<int> RunCode(string code, bool debugMode, LogFactory logFactory, OptimizationLevel optimizationLevel, IEnumerable<string> args, string currentWorkingDirectory, string[] packageSources)
        {
            var sourceText = SourceText.From(code);
            var context = new ScriptContext(sourceText, currentWorkingDirectory ?? Directory.GetCurrentDirectory(), args, null, optimizationLevel, ScriptMode.Eval, packageSources: packageSources);
            return Run(debugMode, logFactory, context);
        }

        private static Task<int> Run(bool debugMode, LogFactory logFactory, ScriptContext context)
        {
            var compiler = GetScriptCompiler(debugMode, logFactory);
            var runner = new ScriptRunner(compiler, logFactory, ScriptConsole.Default);
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

        private static ScriptCompiler GetScriptCompiler(bool debugMode, LogFactory logFactory)
        {
            var runtimeDependencyResolver = new RuntimeDependencyResolver(logFactory);
            var compiler = new ScriptCompiler(logFactory, runtimeDependencyResolver);
            return compiler;
        }
    }
}
