namespace Dotnet.Script.Core
{
    public class ScriptFile
    {
        public ScriptFile(string path)
        {
            Path = path.GetRootedPath();
            Directory = System.IO.Path.GetDirectoryName(Path);
        }

        public string Path {get;}

        public string Directory {get;}
    }    
}

