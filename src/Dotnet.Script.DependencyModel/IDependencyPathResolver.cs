namespace Dotnet.Script.DependencyModel
{
    /// <summary>
    /// Represents a class that is capable of resolving 
    /// the full path to a dependency specified with a relative path.
    /// </summary>
    public interface IDependencyPathResolver
    {
        /// <summary>
        /// Gets the full path of a dependency specified with a relative path.
        /// </summary>
        /// <param name="relativePath">The relative of the dependency to resolve.</param>
        /// <returns>The full path of the dependency if resolvable, otherwise null.</returns>
        string GetFullPath(string relativePath);
    }
}