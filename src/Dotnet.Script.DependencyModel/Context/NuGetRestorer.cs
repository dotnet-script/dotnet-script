using System;
using System.IO;
using System.Reflection;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Process;

namespace Dotnet.Script.DependencyModel.Context
{
    public class NuGetRestorer : IRestorer
    {
        private readonly CommandRunner _commandRunner;
        private readonly Action<bool, string> _logger;
        private static readonly string PathToNuget;

        static NuGetRestorer()
        {
            var directory = Path.GetDirectoryName(new Uri(typeof(NuGetRestorer).GetTypeInfo().Assembly.CodeBase).LocalPath);
            PathToNuget = Path.Combine(directory, "NuGet350.exe");
        }


        public NuGetRestorer(CommandRunner commandRunner, Action<bool, string> logger)
        {
            _commandRunner = commandRunner;
            _logger = logger;
        }

        public void Restore(string pathToProjectFile)
        {
            if (RuntimeHelper.IsWindows())
            {
                _commandRunner.Execute(PathToNuget, $"restore {pathToProjectFile}");
            }
            else
            {
                _commandRunner.Execute("mono", $"{PathToNuget} restore {pathToProjectFile}");
            }
        }

        public bool CanRestore => CheckAvailability();

        private bool CheckAvailability()
        {
            if (RuntimeHelper.IsWindows())
            {
                return _commandRunner.Execute(PathToNuget) == 0;
            }

            return _commandRunner.Execute("mono", PathToNuget) == 0;
        }
    }
}