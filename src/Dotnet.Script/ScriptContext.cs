using System.Collections.Generic;

namespace Dotnet.Script
{
    public class ScriptContext
    {
        public ScriptContext(string file, string config, List<string> scriptArgs)
        {
            FilePath = file;
            Configuration = config;
            ScriptArgs = scriptArgs;
        }

        public string FilePath { get; }

        public string Configuration { get; }

        public List<string> ScriptArgs { get; }
    }
}