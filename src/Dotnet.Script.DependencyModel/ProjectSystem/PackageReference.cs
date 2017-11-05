namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package reference found in a script file.
    /// </summary>
    public class PackageReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        /// <param name="id">The id of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        public PackageReference(string id, string version, PackageOrigin packageOrigin)
        {
            Id = id;
            Version = version;
            Origin = packageOrigin;
        }

        /// <summary>
        /// Gets the id of the NuGet package
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the version of the NuGet package.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the <see cref="PackageOrigin"/> that describes where this reference originated from.
        /// </summary>
        public PackageOrigin Origin { get; }


        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Version.GetHashCode() ^ Origin.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = (PackageReference)obj;
            return other.Id == Id && other.Version == Version && other.Origin == Origin;
        }
    }

    public enum PackageOrigin
    {
        ReferenceDirective,
        LoadDirective
    }
}