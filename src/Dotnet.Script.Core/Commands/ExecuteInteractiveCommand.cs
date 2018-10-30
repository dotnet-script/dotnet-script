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

        public ExecuteInteractiveCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public async Task<int> Execute(ExecuteInteractiveCommandOptions options)
        {
            var compiler = new ScriptCompiler(_logFactory, useRestoreCache: false);
                var runner = new InteractiveRunner(compiler, _logFactory, ScriptConsole.Default, options.PackageSources);

            if (options.ScriptFile != null)
            {
                await runner.RunLoop();
            }
            else
            {
                var context = new ScriptContext(options.ScriptFile.Path.ToSourceText(), Path.GetDirectoryName(options.ScriptFile.Path), options.Arguments, options.ScriptFile.Path, OptimizationLevel.Debug , packageSources: options.PackageSources);
                await runner.RunLoopWithSeed(context);
            }

            return 0;
        }
    }
}