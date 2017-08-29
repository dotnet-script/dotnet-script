using System.Collections.Generic;

namespace Dotnet.Script.Core.Metadata
{
    /// <summary>
    /// Represents a class that is capable of resolving the runtime 
    /// dependencies for a given project file.
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Gets the runtime dependencies for the given <paramref name="projectFile"/>.
        /// </summary>
        /// <param name="projectFile">The project file for which to resolve the runtime dependencies.</param>
        /// <returns></returns>
        IEnumerable<RuntimeDependency> GetRuntimeDependencies(string projectFile);
    }
}