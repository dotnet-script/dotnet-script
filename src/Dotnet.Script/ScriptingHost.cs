using System.Collections.Generic;

namespace Dotnet.Script
{
    public class ScriptingHost
    {
        public IReadOnlyList<string> ScriptArgs { get; internal set; }

        public string CurrentDirectory { get; internal set; }

        public string CurrentScript { get; internal set; }
    }
}