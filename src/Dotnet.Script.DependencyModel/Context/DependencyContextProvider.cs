using System;
using System.IO;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    public class DependencyContextProvider : IDependencyContextProvider
    {
        private readonly IRestorer[] _restorers;
        private readonly Action<bool, string> _logAction;

        public DependencyContextProvider(IRestorer[] restorers, Action<bool, string> logAction)
        {
            _restorers = restorers;
            _logAction = logAction;
        }

        public DependencyContext GetDependencyContext(string pathToProjectFile)
        {
            Restore(pathToProjectFile);
            return ReadDependencyContext(pathToProjectFile);
        }

        private void Restore(string pathToProjectFile)
        {
            foreach (var restorer in _restorers)
            {
                if (restorer.CanRestore)
                {
                    restorer.Restore(pathToProjectFile);
                    return;
                }
            }
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
    }
}