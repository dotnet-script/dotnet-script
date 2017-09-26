using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Runtime
{
    /// <summary>
    /// Represents a class that is capable of resolving the 
    /// dependencies for a given project file.
    /// </summary>
    public interface IRuntimeDependencyResolver
    {
        /// <summary>
        /// Gets the runtime dependencies for the given <paramref name="projectFile"/>.
        /// </summary>
        /// <param name="dependencyContext">The project file for which to resolve the runtime dependencies.</param>
        /// <returns></returns>
        IEnumerable<RuntimeDependency> GetDependencies(DependencyContext dependencyContext);        
    }
}