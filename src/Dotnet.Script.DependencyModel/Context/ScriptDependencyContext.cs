namespace Dotnet.Script.DependencyModel.Context
{
    public class ScriptDependencyContext
    {
        public ScriptDependencyContext(ScriptDependency[] dependencies)
        {
            Dependencies = dependencies;
        }

        public ScriptDependency[] Dependencies { get; }
    }
}