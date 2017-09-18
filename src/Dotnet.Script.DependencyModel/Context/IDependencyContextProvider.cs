using Microsoft.Extensions.DependencyModel;
namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    /// Represents a class that is capable of providing 
    /// the <see cref="DependencyContext"/> for a given project file (csproj).
    /// </summary>
    public interface IDependencyContextProvider
    {
        /// <summary>
        /// Gets the <see cref="DependencyContext"/> for the project file.
        /// </summary>
        /// <param name="pathToProjectFile">The path to the project file.</param>
        /// <returns>The <see cref="DependencyContext"/> for a given project file (csproj).</returns>
        DependencyContext GetDependencyContext(string pathToProjectFile);
    }
}