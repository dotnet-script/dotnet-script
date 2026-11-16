using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteInteractiveCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;
        private readonly Func<LogFactory, IReplLanguageService> _languageServiceFactory;

        public ExecuteInteractiveCommand(ScriptConsole scriptConsole, LogFactory logFactory)
            : this(scriptConsole, logFactory, null)
        {
        }

        public ExecuteInteractiveCommand(ScriptConsole scriptConsole, LogFactory logFactory, Func<LogFactory, IReplLanguageService> languageServiceFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
            _languageServiceFactory = languageServiceFactory;
        }

        public async Task<int> Execute(ExecuteInteractiveCommandOptions options)
        {
            var compiler = new ScriptCompiler(_logFactory, options.CachePath, useRestoreCache: false)
            {
#if NETCOREAPP
                AssemblyLoadContext = options.AssemblyLoadContext
#endif
            };

            // Editor services are optional; the REPL stays fully usable without them.
            using var languageService = _scriptConsole.LineEditor != null ? _languageServiceFactory?.Invoke(_logFactory) : null;
            var runner = new InteractiveRunner(compiler, _logFactory, _scriptConsole, options.PackageSources, languageService);

            if (options.ScriptFile == null)
            {
                await runner.RunLoop();
            }
            else
            {
                var context = new ScriptContext(options.ScriptFile.Path.ToSourceText(), Path.GetDirectoryName(options.ScriptFile.Path), options.Arguments, options.ScriptFile.Path, OptimizationLevel.Debug, packageSources: options.PackageSources);
                await runner.RunLoopWithSeed(context);
            }

            return 0;
        }
    }
}