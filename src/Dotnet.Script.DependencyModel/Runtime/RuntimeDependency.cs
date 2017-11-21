using System.Collections.Generic;

namespace Dotnet.Script.DependencyModel.Runtime
{
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