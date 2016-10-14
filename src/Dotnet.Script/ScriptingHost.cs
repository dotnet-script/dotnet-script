using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script
{
    public class ScriptingHost : InteractiveScriptGlobals
    {
        public ScriptingHost(TextWriter outputWriter, ObjectFormatter objectFormatter)
            : base(outputWriter, objectFormatter)
        {
        }

        public Assembly ScriptAssembly { get; internal set; }
    }
}