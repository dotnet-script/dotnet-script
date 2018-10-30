using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.Runtime;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteScriptCommand : IFileCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;

        public ExecuteScriptCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public async Task<TReturn> Run<TReturn, THost>(ExecuteScriptCommandOptions options)
        {
            if (options.File.IsRemote)
            {
                return await DownloadAndRunCode<TReturn>(options);
            }

            var pathToLibrary = CreateLibrary(options);
            return await ExecuteLibrary<TReturn>(pathToLibrary, options.Arguments, options.NoCache);
        }

        private async Task<TReturn> DownloadAndRunCode<TReturn>(ExecuteScriptCommandOptions executeOptions)
        {
            var downloader = new ScriptDownloader();
            var code = await downloader.Download(executeOptions.File.Path);
            var options = new ExecuteCodeCommandOptions(code, Directory.GetCurrentDirectory(), executeOptions.Arguments, executeOptions.OptimizationLevel, executeOptions.NoCache, executeOptions.PackageSources);
            return await new ExecuteCodeCommand(_scriptConsole, _logFactory).Execute<TReturn>(options);
        }

        private string CreateLibrary(ExecuteScriptCommandOptions executeOptions)
        {
            var projectFolder = FileUtils.GetPathToTempFolder(Path.GetDirectoryName(executeOptions.File.Path));
            var publishDirectory = Path.Combine(projectFolder, "publish");
            var options = new PublishCommandOptions(executeOptions.File,publishDirectory, "script", PublishType.Library,executeOptions.OptimizationLevel, null, executeOptions.NoCache);
            new PublishCommand(_scriptConsole, _logFactory).Execute(options);
            return Path.Combine(publishDirectory, "script.dll");
        }

        private async Task<TReturn> ExecuteLibrary<TReturn>(string pathToLibrary, string[] arguments, bool noCache)
        {
            var options = new ExecuteLibraryCommandOptions(pathToLibrary, arguments, noCache);
            return await new ExecuteLibraryCommand(_scriptConsole, _logFactory).Execute<TReturn>(options);
        }
    }
}