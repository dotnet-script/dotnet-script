using System.Collections.Generic;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{    
    public class ParseResult
    {
        public ParseResult(IReadOnlyCollection<PackageReference> packageReferences, string targetFramework)
        {
            PackageReferences = packageReferences;
            TargetFramework = targetFramework;
        }

        public IReadOnlyCollection<PackageReference> PackageReferences { get; }

        public string TargetFramework { get; }
    }
}