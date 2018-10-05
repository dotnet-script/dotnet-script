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
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            return stringComparer.GetHashCode(Id)
                 ^ stringComparer.GetHashCode(Version)
                 ^ Origin.GetHashCode();
        }

        public bool Equals(PackageReference other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase)
                && Origin == other.Origin;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PackageReference);
        }
    }
}