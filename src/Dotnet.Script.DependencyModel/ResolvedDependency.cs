

namespace Dotnet.Script.DependencyModel
{
    public class ResolvedDependency
    {
        public string Name { get; }
        public string Path { get; }

        public ResolvedDependency(string name, string path)
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
            var other = (ResolvedDependency)obj;
            return other.Name == Name && other.Path == Path;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Path)}: {Path}";
        }
    }
}