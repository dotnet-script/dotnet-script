using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script
{
    public class ScriptCompilationContext<TReturn>
    {
        public Script<TReturn> Script { get; }

        public SourceText SourceText { get; }

        public InteractiveAssemblyLoader Loader { get; }

        public ScriptCompilationContext(Script<TReturn> script, SourceText sourceText, InteractiveAssemblyLoader loader)
        {
            Script = script;
            SourceText = sourceText;
            Loader = loader;
        }
    }
}