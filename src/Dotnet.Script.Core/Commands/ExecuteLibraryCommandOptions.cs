namespace Dotnet.Script.Core.Commands
{
    public class ExecuteLibraryCommandOptions
    {
        public ExecuteLibraryCommandOptions(string libraryPath, string[] arguments, bool noCache, bool noNugetCache)
        {
            LibraryPath = libraryPath;
            Arguments = arguments;
            NoCache = noCache;
            NoNugetCache = noNugetCache;
        }

        public string LibraryPath { get; }
        public string[] Arguments { get; }
        public bool NoCache { get; }
        public bool NoNugetCache { get; }
    }
}