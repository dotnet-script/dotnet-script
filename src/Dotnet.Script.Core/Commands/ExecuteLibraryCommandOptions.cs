namespace Dotnet.Script.Core.Commands
{
    public class ExecuteLibraryCommandOptions
    {
        public ExecuteLibraryCommandOptions(string libraryPath, string[] arguments, bool noCache)
        {
            LibraryPath = libraryPath;
            Arguments = arguments;
            NoCache = noCache;
        }

        public string LibraryPath { get; }
        public string[] Arguments { get; }
        public bool NoCache { get; }
    }
}