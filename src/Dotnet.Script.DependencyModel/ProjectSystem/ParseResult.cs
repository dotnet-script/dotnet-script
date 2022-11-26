using System.Collections.Generic;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public class ParseResult
    {
        public ParseResult(IReadOnlyCollection<PackageReference> packageReferences)
        {
            PackageReferences = packageReferences;
        }

        public IReadOnlyCollection<PackageReference> PackageReferences { get; }

        public string Sdk { get; set; }
    }
}