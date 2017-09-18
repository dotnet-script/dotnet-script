using System;
using System.IO;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Process;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    public class DependencyContextProvider : IDependencyContextProvider
    {
        private readonly Action<bool, string> _logAction;

        public DependencyContextProvider(Action<bool, string> logAction) => _logAction = logAction;


        public DependencyContext GetDependencyContext(string pathToProjectFile)
        {
            Restore(pathToProjectFile);
            return ReadDependencyContext(pathToProjectFile);
        }

        private static DependencyContext ReadDependencyContext(string pathToProjectFile)
        {
            var pathToAssetsFiles = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");

            using (FileStream fs = new FileStream(pathToAssetsFiles, FileMode.Open, FileAccess.Read))
            {
                using (var contextReader = new DependencyContextJsonReader())
                {
                    return contextReader.Read(fs);
                }
            }
        }

        private void Restore(string pathToProjectFile)
        {
            var runtimeId = RuntimeHelper.GetRuntimeIdentifier();
            Command.Execute("dotnet", $"restore {pathToProjectFile} -r {runtimeId}", _logAction);
        }                
    }
}