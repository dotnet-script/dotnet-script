using System;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Represents the version of dotnet-script.
    /// </summary>
    /// <typeparam name="VersionInfo"></typeparam>
    public class VersionInfo : IEquatable<VersionInfo>
    {        
        /// <summary>
        /// Returns a new <see cref="VersionInfo"/> that is considered "unresolved".
        /// </summary>
        /// <param name="false"></param>
        /// <returns></returns>
        public static VersionInfo UnResolved = new VersionInfo(string.Empty, isResolved: false);
    
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionInfo"/> class. 
        /// </summary>
        /// <param name="version">The version represented a string.</param>
        /// <param name="isResolved">Indicates if the version information was successfully resolved.</param>
        public VersionInfo(string version, bool isResolved)
        {
            Version = version;
            IsResolved = isResolved;
        }

        /// <summary>
        /// Gets the version represented as a string.
        /// </summary>
        /// <value></value>
        public string Version { get; }
        
        /// <summary>
        /// Gets a <see cref="bool"/> value that indicates if the version information 
        /// was successfully resolved.
        /// </summary>
        /// <value></value>
        public bool IsResolved { get; }

        /// <inheritdoc/>
        public bool Equals(VersionInfo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Version, other.Version) 
                && IsResolved == other.IsResolved;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            return stringComparer.GetHashCode(Version) 
                ^ IsResolved.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as VersionInfo);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Version;
        }
    }
}