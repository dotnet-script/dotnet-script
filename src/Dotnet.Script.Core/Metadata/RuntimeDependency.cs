using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Metadata
{
    public class RuntimeDependency
    {
        public string Name { get; }
        public string Path { get; }
        public AssemblyName AssemblyName {get;}

        public RuntimeDependency(string name, string path)
        {
            Name = name;
            Path = path;
            AssemblyName = AssemblyLoadContext.GetAssemblyName(path);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Path.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = (RuntimeDependency)obj;
            return other.Name == Name && other.Path == Path;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Path)}: {Path}";
        }
    }
}