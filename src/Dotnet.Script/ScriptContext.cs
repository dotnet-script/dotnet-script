using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script
{
    public class ScriptContext
    {
        public ScriptContext(SourceText code, string workingDirectory, string config, IEnumerable<string> args, string filePath = null)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Configuration = config;
            Args = new ReadOnlyCollection<string>(args.ToArray());
            FilePath = filePath;
        }

        public SourceText Code { get; }

        public string WorkingDirectory { get; }

        public string Configuration { get; }

        public IReadOnlyList<string> Args { get; }

        public string FilePath { get; }
    }
}