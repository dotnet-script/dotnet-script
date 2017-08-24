using System.Collections.Generic;
using NuGet.Packaging;

namespace Dotnet.Script.Core.ProjectSystem
{
    /// <summary>
    /// Represents the result of parsing a set of script files.
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseResult"/> class.
        /// </summary>
        /// <param name="packageReferences">A list of <see cref="PackageReference"/> instances that represents 
        /// references to NuGet packages in a given set of script files.</param>
        /// <param name="targetFramework">The target framework inferred from the #! directive.</param>
        public ParseResult(IReadOnlyCollection<PackageReference> packageReferences, string targetFramework)
        {
            PackageReferences = packageReferences;
            TargetFramework = targetFramework;
        }

        /// <summary>
        /// Gets a list of NuGet package references found within a set of script files.
        /// </summary>
        public IReadOnlyCollection<PackageReference> PackageReferences { get; }

        /// <summary>
        /// Gets the target framework inferred from the #! directive.
        /// </summary>
        public string TargetFramework { get; }
    }
}