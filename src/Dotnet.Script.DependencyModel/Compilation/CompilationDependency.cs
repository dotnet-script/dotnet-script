using System.Collections.Generic;

public class CompilationDependency
{
    public CompilationDependency(string name, string version, IReadOnlyList<string> assemblyPaths, IReadOnlyList<string> scripts)
    {
        Name = name;
        Version = version;
        AssemblyPaths = assemblyPaths;
        Scripts = scripts;
    }

    public string Name { get; }

    public string Version { get; }

    public IReadOnlyList<string> AssemblyPaths { get; }

    public IReadOnlyList<string> Scripts { get; }

    public override string ToString()
    {
        return $"Name: {Name} , Version: {Version}";
    }
}