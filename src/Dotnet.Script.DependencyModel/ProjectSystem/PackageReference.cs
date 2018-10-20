using System;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package reference found in a script file.
    /// </summary>
    public class PackageReference : IEquatable<PackageReference>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        /// <param name="id">The id of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        public PackageReference(string id, string version)
        {
            Id = new PackageId(id);
            Version = new PackageVersion(version);
        }

        /// <summary>
        /// Gets the <see cref="PackageId"/> of the NuGet package
        /// </summary>
        public PackageId Id { get; }

        /// <summary>
        /// Gets the <see cref="PackageVersion"/> of the NuGet package.
        /// </summary>
        public PackageVersion Version { get; }

        /// <inheritdoc />
        public override int GetHashCode()
        {
           return (Id, Version).GetHashCode();
        }

        /// <inheritdoc />
        public bool Equals(PackageReference other)
        {
           return (Id, Version).Equals((other.Id, other.Version));
        }
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as PackageReference);
        }
    }
}