using System;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;

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

        public void Restore(string pathToProjectFile)
        {
            var runtimeIdentifier = _scriptEnvironment.RuntimeIdentifier;
            _logger.Debug($"Restoring {pathToProjectFile} using the dotnet cli. RuntimeIdentifier : {runtimeIdentifier}");            
            var exitcode = _commandRunner.Execute("dotnet", $"restore \"{pathToProjectFile}\" -r {runtimeIdentifier}");
            if (exitcode != 0)
            {
                // We must throw here, otherwise we may incorrectly run with the old 'project.assets.json'
                throw new Exception($"Unable to restore packages from '{pathToProjectFile}'. Make sure that all script files contains valid NuGet references");
            }
        }

        public bool CanRestore => _commandRunner.Execute("dotnet", "--version") == 0;
    }
}