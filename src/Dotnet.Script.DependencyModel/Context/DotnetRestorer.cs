using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Microsoft.Extensions.DependencyModel;
using System;
using System.IO;
using System.Linq;

namespace Dotnet.Script.DependencyModel.Context
{
    public class DotnetRestorer : IRestorer
    {
        private readonly CommandRunner _commandRunner;
        private readonly Logger _logger;
        private readonly ScriptEnvironment _scriptEnvironment;

        public DotnetRestorer(CommandRunner commandRunner, LogFactory logFactory)
        {
            _commandRunner = commandRunner;
            _logger = logFactory.CreateLogger<DotnetRestorer>();
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public void Restore(string pathToProjectFile, string[] packageSources)
        {
            var packageSourcesArgument = CreatePackageSourcesArguments();
            var runtimeIdentifier = _scriptEnvironment.RuntimeIdentifier;

             _logger.Debug($"Restoring {pathToProjectFile} using the dotnet cli. RuntimeIdentifier : {runtimeIdentifier}");
            var exitcode = _commandRunner.Execute("dotnet", $"restore \"{pathToProjectFile}\" -r {runtimeIdentifier} {packageSourcesArgument}");
            if (exitcode != 0)
            {
                // We must throw here, otherwise we may incorrectly run with the old 'project.assets.json'
                throw new Exception($"Unable to restore packages from '{pathToProjectFile}'. Make sure that all script files contains valid NuGet references");
            }


            var pathToPublishFolder = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "publish");
             _logger.Debug($"Restoring {pathToProjectFile} using the dotnet cli. RuntimeIdentifier : {runtimeIdentifier}");
            exitcode = _commandRunner.Execute("dotnet", $"publish \"{pathToProjectFile}\" --no-restore -o {pathToPublishFolder} {packageSourcesArgument}");
            if (exitcode != 0)
            {
                // We must throw here, otherwise we may incorrectly run with the old 'project.assets.json'
                throw new Exception($"Unable to restore packages from '{pathToProjectFile}'. Make sure that all script files contains valid NuGet references");
            }


            var pathToDepsFile = Path.Combine(pathToPublishFolder,"script.deps.json");
            using (var contextReader = new DependencyContextJsonReader())
            {
                var dependencyContext = contextReader.Read(pathToDepsFile);
                var nativeAssets = dependencyContext.GetRuntimeNativeAssets(ScriptEnvironment.Default.RuntimeIdentifier);
                foreach (var nativeAsset in nativeAssets)
                {
                    var fullPathToNativeAsset = Path.Combine(pathToPublishFolder,nativeAsset);
                    if (File.Exists(fullPathToNativeAsset))
                    {
                        var destinationPath = Path.Combine(pathToPublishFolder, Path.GetFileName(fullPathToNativeAsset));
                        File.Copy(fullPathToNativeAsset, destinationPath, overwrite:true);
                    }
                }
            }



            string CreatePackageSourcesArguments()
            {
                return packageSources.Length == 0
                    ? string.Empty
                    : packageSources.Select(s => $"-s {s}")
                        .Aggregate((current, next) => $"{current} {next}");
            }
        }

        public bool CanRestore => true;
    }
}