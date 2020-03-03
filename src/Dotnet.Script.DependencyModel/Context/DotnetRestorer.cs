using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using System;
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

        public void Restore(ProjectFileInfo projectFileInfo, string[] packageSources)
        {
            var packageSourcesArgument = CreatePackageSourcesArguments();
            var configFileArgument = CreateConfigFileArgument();
            var runtimeIdentifier = _scriptEnvironment.RuntimeIdentifier;

            _logger.Debug($"Restoring {projectFileInfo.Path} using the dotnet cli. RuntimeIdentifier : {runtimeIdentifier} NugetConfigFile: {projectFileInfo.NuGetConfigFile}");
            var exitcode = _commandRunner.Execute("dotnet", $"restore \"{projectFileInfo.Path}\" -r {runtimeIdentifier} {packageSourcesArgument} {configFileArgument}");
            if (exitcode != 0)
            {
                // We must throw here, otherwise we may incorrectly run with the old 'project.assets.json'
                throw new Exception($"Unable to restore packages from '{projectFileInfo.Path}'. Make sure that all script files contains valid NuGet references");
            }

            string CreatePackageSourcesArguments()
            {
                return packageSources.Length == 0
                    ? string.Empty
                    : packageSources.Select(s => $"-s {s}")
                        .Aggregate((current, next) => $"{current} {next}");
            }

            string CreateConfigFileArgument()
            {
                return string.IsNullOrWhiteSpace(projectFileInfo.NuGetConfigFile)
                    ? string.Empty
                    : $"--configfile \"{projectFileInfo.NuGetConfigFile}\"";

            }
        }

        public bool CanRestore => true;
    }
}