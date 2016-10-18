using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script
{
    public class ScriptCompilationContext<TReturn>
    {
        public Compilation Compilation { get; }
        public Script<TReturn> Script { get; }
        public InteractiveScriptGlobals Host { get; }
        public SourceText SourceText { get; }
        public InteractiveAssemblyLoader Loader { get; }

        public ScriptCompilationContext(Compilation compilation, Script<TReturn> script, InteractiveScriptGlobals host, SourceText sourceText, InteractiveAssemblyLoader loader)
        {
            Compilation = compilation;
            Script = script;
            Host = host;
            SourceText = sourceText;
            Loader = loader;
        }
    }
}