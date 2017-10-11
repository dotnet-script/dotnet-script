namespace Dotnet.Script.DependencyModel.Context
{
    /// <summary>
    /// Represents a class that is capable of restoring a project file.
    /// </summary>
    public interface IRestorer
    {
        /// <summary>
        /// Restores the dependencies specified in the given project file.
        /// </summary>
        /// <param name="pathToProjectFile"></param>
        void Restore(string pathToProjectFile);

        /// <summary>
        /// Gets a <see cref="bool"/> value that indicates if this <see cref="IRestorer"/> is available on the system.
        /// </summary>
        bool CanRestore { get; }
    }
}