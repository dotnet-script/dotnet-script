using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.Core.Internal;
using Microsoft.DotNet.ProjectModel;

namespace Dotnet.Script.Core.Metadata
{
    /// <summary>
    /// An <see cref="IDependencyResolver"/> that resolves runtime dependencies 
    /// from an "project.json" file.
    /// </summary>
    public class LegacyDependencyResolver : IDependencyResolver
    {
        private ScriptLogger _logger;


        public LegacyDependencyResolver(ScriptLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<RuntimeDependency> GetRuntimeDependencies(string projectFile)
        {
            var workingDirectory = Path.GetDirectoryName(projectFile);
            var runtimeContext = ProjectContext.CreateContextForEachTarget(workingDirectory).FirstOrDefault();
            var projectExporter = runtimeContext.CreateExporter("release");
            var runtimeIdentifier = RuntimeHelper.GetRuntimeIdentitifer();

            var runtimeDependencies = new HashSet<RuntimeDependency>();
            var projectDependencies = projectExporter.GetDependencies();
            foreach (var projectDependency in projectDependencies)
            {
                var runtimeAssemblyGroups = projectDependency.RuntimeAssemblyGroups;

                foreach (var libraryAsset in runtimeAssemblyGroups.GetDefaultAssets())
                {
                    var runtimeAssemblyPath = libraryAsset.ResolvedPath;
                    _logger.Verbose($"Discovered runtime dependency for '{runtimeAssemblyPath}'");
                    var runtimeDependency = new RuntimeDependency(libraryAsset.Name, libraryAsset.ResolvedPath);
                    runtimeDependencies.Add(runtimeDependency);
                }

                foreach (var runtimeAssemblyGroup in runtimeAssemblyGroups)
                {
                    if (!string.IsNullOrWhiteSpace(runtimeAssemblyGroup.Runtime) && runtimeAssemblyGroup.Runtime == runtimeIdentifier)
                    {
                        foreach (var runtimeAsset in runtimeAssemblyGroups.GetRuntimeAssets(runtimeIdentifier))
                        {
                            var runtimeAssetPath = runtimeAsset.ResolvedPath;
                            _logger.Verbose($"Discovered runtime asset dependency ('{runtimeIdentifier}') for '{runtimeAssetPath}'");
                            var runtimeDependency = new RuntimeDependency(runtimeAsset.Name, runtimeAsset.ResolvedPath);
                            runtimeDependencies.Add(runtimeDependency);
                        }
                    }
                }
            }

            return runtimeDependencies;
        }
    }
}