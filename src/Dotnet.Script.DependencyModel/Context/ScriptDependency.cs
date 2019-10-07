namespace Dotnet.Script.DependencyModel.Context
{
    public class ScriptDependency
    {
        public ScriptDependency(string name, string version, string[] runtimeDependencyPaths, string[] nativeAssetPaths, string[] compileTimeDependencyPaths, string[] scriptPaths)
        {
            Name = name;
            Version = version;
            RuntimeDependencyPaths = runtimeDependencyPaths;
            NativeAssetPaths = nativeAssetPaths;
            CompileTimeDependencyPaths = compileTimeDependencyPaths;
            ScriptPaths = scriptPaths;
        }

        public string Name { get; }
        public string Version { get; }
        public string[] RuntimeDependencyPaths { get; }
        public string[] NativeAssetPaths { get; }
        public string[] CompileTimeDependencyPaths { get; }
        public string[] ScriptPaths { get; }

        public override string ToString()
        {
            return $"{Name}, {Version}";
        }
    }
}