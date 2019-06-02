using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace Dotnet.Script.Core
{
    public class ScriptCompilationContext<TReturn>
    {
        public Script<TReturn> Script { get; }

        public SourceText SourceText { get; }

        public InteractiveAssemblyLoader Loader { get; }

        public ScriptOptions ScriptOptions { get; }

        public RuntimeDependency[] RuntimeDependencies { get; }

        public Diagnostic[] Warnings { get; }

        public Diagnostic[] Errors { get; }

        public ScriptCompilationContext(Script<TReturn> script, SourceText sourceText, InteractiveAssemblyLoader loader, ScriptOptions scriptOptions, RuntimeDependency[] runtimeDependencies, Diagnostic[] diagnostics)
        {
            Script = script;
            SourceText = sourceText;
            ScriptOptions = scriptOptions;
            Loader = loader;
            RuntimeDependencies = runtimeDependencies;

            Warnings = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).ToArray();
            Errors = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
        }
    }
}