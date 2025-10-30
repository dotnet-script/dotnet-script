using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteCodeCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;

        public ExecuteCodeCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public async Task<TReturn> Execute<TReturn>(ExecuteCodeCommandOptions options)
        {
            var sourceText = SourceText.From(options.Code);
            var context = new ScriptContext(sourceText, options.WorkingDirectory ?? Directory.GetCurrentDirectory(), options.Arguments, null, options.OptimizationLevel, ScriptMode.Eval, options.PackageSources);
            var compiler = new ScriptCompiler(_logFactory, options.CachePath, !options.NoCache)
            {
#if NETCOREAPP
                AssemblyLoadContext = options.AssemblyLoadContext
#endif
            };
            var runner = new ScriptRunner(compiler, _logFactory, _scriptConsole)
            {
#if NETCOREAPP
                AssemblyLoadContext = options.AssemblyLoadContext
#endif
            };
            return await runner.Execute<TReturn>(context);
        }
    }
}