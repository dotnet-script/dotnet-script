

namespace Dotnet.Script.DependencyModel.Runtime
{
    public class RuntimeDependency
    {
        public string Name { get; }
        public string Path { get; }

        public RuntimeDependency(string name, string path)
        {
            Name = name;
            Path = path;           
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