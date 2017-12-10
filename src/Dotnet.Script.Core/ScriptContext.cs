using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core
{
    public class ScriptContext
    {
        public ScriptContext(SourceText code, string workingDirectory, IEnumerable<string> args, string filePath = null, bool debugMode = false)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Args = new ReadOnlyCollection<string>(args.ToArray());
            FilePath = filePath;
            DebugMode = debugMode;
        }

        public SourceText Code { get; }

        public string WorkingDirectory { get; }

        public IReadOnlyList<string> Args { get; }

        public string FilePath { get; }

        public bool DebugMode { get; }
    }
}