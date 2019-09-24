namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Contains information about the generated project file and
    /// where to find the "nearest" NuGet.Config file.
    /// </summary>
    public class ProjectFileInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFileInfo"/> class.
        /// </summary>
        /// <param name="path">The path to the generated project file to be used during restore.</param>
        /// <param name="nugetConfigFile">The path to the nearest NuGet.Config file seen from the target script folder.</param>
        public ProjectFileInfo(string path, string nugetConfigFile)
        {
            Path = path;
            NuGetConfigFile = nugetConfigFile;
        }

        /// <summary>
        /// Gets the path of the generated project file to be used during restore.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the path to the nearest NuGet.Config file seen from the target script folder.
        /// </summary>
        public string NuGetConfigFile { get; }
    }
}