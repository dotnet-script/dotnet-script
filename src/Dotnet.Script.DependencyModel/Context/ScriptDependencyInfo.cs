using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    public class ScriptDependencyInfo
    {
        public ScriptDependencyInfo(DependencyContext dependencyContext, string[] nugetPackageFolders)
        {
            DependencyContext = dependencyContext;
            NugetPackageFolders = nugetPackageFolders;
        }

        public DependencyContext DependencyContext { get; }
        public string[] NugetPackageFolders { get; }
    }
}