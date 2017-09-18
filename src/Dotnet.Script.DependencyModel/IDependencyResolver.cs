using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel
{
    /// <summary>
    /// Represents a class that is capable of resolving the 
    /// dependencies for a given project file.
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Gets the runtime dependencies for the given <paramref name="projectFile"/>.
        /// </summary>
        /// <param name="dependencyContext">The project file for which to resolve the runtime dependencies.</param>
        /// <returns></returns>
        IEnumerable<ResolvedDependency> GetDependencies(DependencyContext dependencyContext);        
    }
}