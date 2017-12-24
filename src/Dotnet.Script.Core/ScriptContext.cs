using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dotnet.Script.DependencyModel.Context;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core
{
    public class ScriptContext
    {
        public ScriptContext(SourceText code, string workingDirectory, IEnumerable<string> args, string filePath = null, bool debugMode = false, ScriptMode scriptMode = ScriptMode.Script)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Args = new ReadOnlyCollection<string>(args.ToArray());
            FilePath = filePath;
            DebugMode = debugMode;
            ScriptMode = filePath != null ? ScriptMode.Script : scriptMode;
        }

        public SourceText Code { get; }

        public string WorkingDirectory { get; }

        public IReadOnlyList<string> Args { get; }

        public string FilePath { get; }

        public bool DebugMode { get; }

        public ScriptMode ScriptMode { get; }
    }
}