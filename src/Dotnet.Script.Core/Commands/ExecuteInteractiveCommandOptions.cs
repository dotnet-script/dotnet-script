using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core.Commands
{
    public class ExecuteInteractiveCommandOptions
    {
        public ExecuteInteractiveCommandOptions(ScriptFile scriptFile, string[] arguments ,string[] packageSources)
        {
            ScriptFile = scriptFile;
            Arguments = arguments;
            PackageSources = packageSources;
        }

        public ScriptFile ScriptFile { get; }
        public string[] Arguments { get; }
        public string[] PackageSources { get; }
    }
}