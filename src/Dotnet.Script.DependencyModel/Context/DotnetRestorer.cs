using System;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;

namespace Dotnet.Script.DependencyModel.Context
{
    public class DotnetRestorer : IRestorer
    {
        private readonly CommandRunner _commandRunner;
        private readonly Action<bool, string> _logger;

        public DotnetRestorer(CommandRunner commandRunner, Action<bool, string> logger)
        {
            _commandRunner = commandRunner;
            _logger = logger;
        }

        public void Restore(string pathToProjectFile)
        {
            _logger.Verbose($"Restoring {pathToProjectFile} using the dotnet cli.");            
            _commandRunner.Execute("dotnet", $"restore {pathToProjectFile}");            
        }

        public bool CanRestore => _commandRunner.Execute("dotnet", "--version") == 0;
    }
}