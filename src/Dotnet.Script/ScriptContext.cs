using System.Collections.Generic;

namespace Dotnet.Script
{
    public class ScriptContext
    {
        public ScriptContext(string file, string config, bool debugMode, List<string> scriptArgs)
        {
            FilePath = file;
            Configuration = config;
            DebugMode = debugMode;
            ScriptArgs = scriptArgs;
        }

        public string FilePath { get; }

        public string Configuration { get; }

        public bool DebugMode { get; }

        public List<string> ScriptArgs { get; }
    }
}