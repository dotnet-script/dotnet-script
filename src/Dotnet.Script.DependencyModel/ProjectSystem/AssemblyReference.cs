using System;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents an assembly reference found in a script file.
    /// </summary>
    public class AssemblyReference : IEquatable<AssemblyReference>
    {
        private static readonly StringComparer PathComparer = ScriptEnvironment.Default.IsWindows
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyReference"/> class.
        /// </summary>
        /// <param name="assemblyPath">The path of the referenced assembly.</param>
        public AssemblyReference(string assemblyPath) => AssemblyPath = assemblyPath;

        /// <summary>
        /// Gets the path of the referenced assembly.
        /// </summary>
        public string AssemblyPath { get; }

        public bool Equals(AssemblyReference other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return PathComparer.Equals(AssemblyPath, other.AssemblyPath);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as AssemblyReference);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return PathComparer.GetHashCode(AssemblyPath);
        }
    }
}