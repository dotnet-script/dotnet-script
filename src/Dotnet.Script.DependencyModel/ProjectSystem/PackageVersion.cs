using System;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents a NuGet package version.
    /// </summary>
    public class PackageVersion : IEquatable<PackageVersion>
    {
        // Following patterns are "inspired" from SemVer 2.0 grammar:
        //
        // Source: https://semver.org/spec/v2.0.0.html#backusnaur-form-grammar-for-valid-semver-versions

        //   <numeric identifier> ::= "0"
        //                          | <positive digit>
        //                          | <positive digit> <digits>
        //
        //   <digits> ::= <digit>
        //              | <digit> <digits>
        //
        //   <digit> ::= "0"
        //             | <positive digit>
        //
        //   <positive digit> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"

        const string NumericPattern = @"(?:0|[1-9][0-9]*)";

        //   <valid semver> ::= <version core>
        //          | <version core> "-" <pre-release>
        //          | <version core> "+" <build>
        //          | <version core> "-" <pre-release> "+" <build>
        //
        //   <version core> ::= <major> "." <minor> "." <patch>

        const string MajorPlusVersionPattern = NumericPattern + @"(?:\." + NumericPattern + @")";
        const string VersionSuffixPattern = @"(?:[+-][\w][\w+.-]*)?";

        private static readonly Regex IsPinnedRegex =
            new Regex(@"^(?>\[" + MajorPlusVersionPattern + @"{1,4}" + VersionSuffixPattern + @"\]"
                         + @"|" + MajorPlusVersionPattern + @"{2,3}" + VersionSuffixPattern + @")$",
                      RegexOptions.Compiled);

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