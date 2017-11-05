using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core
{
    public class ScriptCompilationContext<TReturn>
    {
        public Script<TReturn> Script { get; }

        public SourceText SourceText { get; }

        public InteractiveAssemblyLoader Loader { get; }

        public ScriptOptions ScriptOptions { get; }

        public ScriptCompilationContext(Script<TReturn> script, SourceText sourceText, InteractiveAssemblyLoader loader, ScriptOptions scriptOptions)
        {
            Script = script;
            SourceText = sourceText;
            ScriptOptions = scriptOptions;
            Loader = loader;
        }
    }
}