using System.Collections.Generic;
using System.Reflection;

namespace Dotnet.Script.DependencyModel.Runtime
{
    public class RuntimeAssembly
    {
        public AssemblyName Name { get; }
        public string Path { get; }       

        public RuntimeAssembly(AssemblyName name, string path)
        {
            Name = name;
            Path = path;
        }
                
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Path.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = (RuntimeAssembly)obj;
            return other.Name == Name && other.Path == Path;
        }
        
        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Path)}: {Path}";
        }
    }

    public class RuntimeDependency
    {
        public RuntimeDependency(string name, string version, IReadOnlyList<RuntimeAssembly> assemblies, IReadOnlyList<string> nativeAssets, IReadOnlyList<string> scripts)
        {
            Name = name;
            Version = version;
            Assemblies = assemblies;
            NativeAssets = nativeAssets;
            Scripts = scripts;            
        }

        public string Name { get; }

        public string Version { get;}

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeAssets { get; }

        public IReadOnlyList<string> Scripts { get; }        
    }
}