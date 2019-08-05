using System;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package version.
    /// </summary>
    public class PackageVersion : IEquatable<PackageVersion>
    {
        private static Regex IsPinnedRegex = new Regex(@"^(?>\[\d+[^,\]]+(?<!\.)\]|\d+(\.\d+){2,})$", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageVersion"/> class.
        /// </summary>
        /// <param name="version">The string representation of the package version.</param>
        public PackageVersion(string version)
        {
            Value = version;
            IsPinned = IsPinnedRegex.IsMatch(version);
        }

        /// <summary>
        /// Gets the package version as a string.
        /// </summary>
        /// <value></value>
        public string Value { get; }

        /// <summary>
        /// Gets a <see cref="bool"/> value that indicates whether the <see cref="PackageVersion"/> is "pinned".
        /// </summary>
        public bool IsPinned { get; }

        /// <inheritdoc/>
        public bool Equals(PackageVersion other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as PackageVersion);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }
    }
}