using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.Runtime;

namespace Dotnet.Script.Core.Commands
{
    public class FileCommand : IFileCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;

        public FileCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public async Task<TReturn> Run<TReturn, THost>(FileCommandOptions options)
        {
            var projectFolder = FileUtils.GetPathToTempFolder(options.File.Path);
            var publishDirectory = Path.Combine(projectFolder, "publish");
            string pathToDll = Path.Combine(publishDirectory, "script.dll");

            var compiler = GetScriptCompiler(!options.NoCache, _logFactory);
            var runtimeIdentifier = ScriptEnvironment.Default.RuntimeIdentifier;
            var scriptEmitter = new ScriptEmitter(ScriptConsole.Default, compiler);
            var publisher = new ScriptPublisher(_logFactory, scriptEmitter);
            var context = new ScriptContext(options.File.Path.ToSourceText(), publishDirectory, Enumerable.Empty<string>(), options.File.Path, options.OptimizationLevel);

            publisher.CreateAssembly<TReturn,THost>(context, _logFactory, Path.GetFileNameWithoutExtension(pathToDll));

            var runner = new ScriptRunner(compiler, _logFactory, ScriptConsole.Default);
            var result = await runner.Execute<TReturn>(pathToDll, options.Arguments);
            return result;
        }

        private static ScriptCompiler GetScriptCompiler(bool useRestoreCache, LogFactory logFactory)
        {
            var runtimeDependencyResolver = new RuntimeDependencyResolver(logFactory, useRestoreCache);
            var compiler = new ScriptCompiler(logFactory, runtimeDependencyResolver);
            return compiler;
        }
    }
}