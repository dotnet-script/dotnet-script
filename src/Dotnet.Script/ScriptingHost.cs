using System.Collections.Generic;
using System.Reflection;

namespace Dotnet.Script
{
    public class ScriptingHost
    {
        public IReadOnlyList<string> Args { get; internal set; }

        public string ScriptDirectory { get; internal set; }

        public string ScriptPath { get; internal set; }

        public Assembly ScriptAssembly { get; internal set; }
    }
}