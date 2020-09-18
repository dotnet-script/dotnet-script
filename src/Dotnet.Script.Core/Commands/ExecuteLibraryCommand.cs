using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteLibraryCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;

        public ExecuteLibraryCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public async Task<TReturn> Execute<TReturn>(ExecuteLibraryCommandOptions options)
        {
            if (!File.Exists(options.LibraryPath))
            {
                throw new Exception($"Couldn't find file '{options.LibraryPath}'");
            }

            var absoluteFilePath = options.LibraryPath.GetRootedPath();
            var compiler = GetScriptCompiler(!options.NoCache, !options.NoNugetCache, _logFactory);
            var runner = new ScriptRunner(compiler, _logFactory, _scriptConsole);
            var result = await runner.Execute<TReturn>(absoluteFilePath, options.Arguments);
            return result;
        }

        private static ScriptCompiler GetScriptCompiler(bool useRestoreCache, bool useNugetCache, LogFactory logFactory)
        {
            var compiler = new ScriptCompiler(logFactory, useRestoreCache, useNugetCache);
            return compiler;
        }
    }
}