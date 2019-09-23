namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationReference
    {
        public CompilationReference(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}