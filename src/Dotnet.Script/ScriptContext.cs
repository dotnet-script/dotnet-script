using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script
{
    public class ScriptContext
    {
        public ScriptContext(SourceText code, string workingDirectory, string config, List<string> scriptArgs, string filePath = null)
        {
            Code = code;
            WorkingDirectory = workingDirectory;
            Configuration = config;
            ScriptArgs = scriptArgs;
            FilePath = filePath;
        }

        public SourceText Code { get; }

        public string WorkingDirectory { get; }

        public string Configuration { get; }

        public List<string> ScriptArgs { get; }

        public string FilePath { get; }
    }
}