using System;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package identifier.
    /// </summary>
    /// <typeparam name="PackageName"></typeparam>
    public class PackageId : IEquatable<PackageId>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageId"/> class.
        /// /// </summary>
        /// <param name="value">The ID of the Nuget package</param>
        public PackageId(string value) => Value = value;

        /// <summary>
        /// Gets the ID of the package.
        /// </summary>
        public string Value { get; }

        /// <inheritdoc/>
        public bool Equals(PackageId other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as PackageId);
        }
    }
}