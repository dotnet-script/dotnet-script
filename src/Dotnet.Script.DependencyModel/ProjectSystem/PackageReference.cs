using System;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package reference found in a script file.
    /// </summary>
    public class PackageReference : IEquatable<PackageReference>
    {
        const string IsPinnedPattern = @"^\d+.\d+.\d+$";

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        /// <param name="id">The id of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        public PackageReference(string id, string version)
        {
            Id = id;
            Version = version;
            IsPinned = Regex.IsMatch(version, IsPinnedPattern);
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
        /// Gets a <see cref="bool"/> value that indicates whether the <see cref="Version"/> is "pinned".
        /// </summary>
        /// <value></value>
        public bool IsPinned {get;}

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            return stringComparer.GetHashCode(Id)
                 ^ stringComparer.GetHashCode(Version);
        }

        public bool Equals(PackageReference other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PackageReference);
        }
    }
}