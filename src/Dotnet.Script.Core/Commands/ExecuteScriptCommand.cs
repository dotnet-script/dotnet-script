using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteScriptCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;
        private readonly Logger _logger;

        public ExecuteScriptCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
            _logger = logFactory.CreateLogger<ExecuteScriptCommand>();

        }

        public async Task<TReturn> Run<TReturn, THost>(ExecuteScriptCommandOptions options)
        {
            if (options.File.IsRemote)
            {
                return await DownloadAndRunCode<TReturn>(options);
            }

            var pathToLibrary = GetLibrary(options);
            return await ExecuteLibrary<TReturn>(pathToLibrary, options.Arguments, options.NoCache);
        }

        private async Task<TReturn> DownloadAndRunCode<TReturn>(ExecuteScriptCommandOptions executeOptions)
        {
            var downloader = new ScriptDownloader();
            var code = await downloader.Download(executeOptions.File.Path);
            var options = new ExecuteCodeCommandOptions(code, Directory.GetCurrentDirectory(), executeOptions.Arguments, executeOptions.OptimizationLevel, executeOptions.NoCache, executeOptions.PackageSources);
            return await new ExecuteCodeCommand(_scriptConsole, _logFactory).Execute<TReturn>(options);
        }

        private string GetLibrary(ExecuteScriptCommandOptions executeOptions)
        {
            var projectFolder = FileUtils.GetPathToScriptTempFolder(executeOptions.File.Path);
            var executionCacheFolder = Path.Combine(projectFolder, "execution-cache");
            var pathToLibrary = Path.Combine(executionCacheFolder, "script.dll");

            if (TryCreateHash(executeOptions, out var hash)
                && TryGetHash(executionCacheFolder, out var cachedHash)
                && string.Equals(hash, cachedHash))
            {
                _logger.Debug($"Using cached compilation: " + pathToLibrary);
                return pathToLibrary;
            }

            var options = new PublishCommandOptions(executeOptions.File, executionCacheFolder, "script", PublishType.Library, executeOptions.OptimizationLevel, executeOptions.PackageSources, null, executeOptions.NoCache);
            new PublishCommand(_scriptConsole, _logFactory).Execute(options);
            if (hash != null)
            {
                File.WriteAllText(Path.Combine(executionCacheFolder, "script.sha256"), hash);
            }
            return Path.Combine(executionCacheFolder, "script.dll");
        }

        public bool TryCreateHash(ExecuteScriptCommandOptions options, out string hash)
        {
            if (options.NoCache)
            {
                _logger.Debug($"The script {options.File.Path} was executed with the '--no-cache' flag. Skipping cache.");
                hash = null;
                return false;
            }

            var scriptFilesProvider = new ScriptFilesResolver();
            var allScriptFiles = scriptFilesProvider.GetScriptFiles(options.File.Path);
            var projectFile = new ScriptProjectProvider(_logFactory).CreateProjectFileFromScriptFiles(ScriptEnvironment.Default.TargetFramework, allScriptFiles.ToArray());

            if (!projectFile.IsCacheable)
            {
                _logger.Warning($"The script {options.File.Path} is not cacheable. For caching and optimal performance, ensure that the script only contains NuGet references with pinned/exact versions.");
                hash = null;
                return false;
            }


            IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var scriptFile in allScriptFiles)
            {
                incrementalHash.AppendData(File.ReadAllBytes(scriptFile));
            }

            var configuration = options.OptimizationLevel.ToString();
            incrementalHash.AppendData(Encoding.UTF8.GetBytes(configuration));

            // Ensure that we don't run with the deps of an old target framework or SDK version.
            incrementalHash.AppendData(Encoding.UTF8.GetBytes(ScriptEnvironment.Default.NetCoreVersion.Tfm));
            incrementalHash.AppendData(Encoding.UTF8.GetBytes(ScriptEnvironment.Default.NetCoreVersion.Version));

            hash = Convert.ToBase64String(incrementalHash.GetHashAndReset());
            return true;
        }


        public bool TryGetHash(string cacheFolder, out string hash)
        {
            if (!Directory.Exists(cacheFolder))
            {
                hash = null;
                return false;
            }

            var pathToHashFile = Path.Combine(cacheFolder, "script.sha256");

            if (!File.Exists(Path.Combine(cacheFolder, "script.sha256")))
            {
                hash = null;
                return false;
            }

            hash = File.ReadAllText(pathToHashFile);
            return true;
        }

        private async Task<TReturn> ExecuteLibrary<TReturn>(string pathToLibrary, string[] arguments, bool noCache)
        {
            var options = new ExecuteLibraryCommandOptions(pathToLibrary, arguments, noCache);
            return await new ExecuteLibraryCommand(_scriptConsole, _logFactory).Execute<TReturn>(options);
        }
    }
}