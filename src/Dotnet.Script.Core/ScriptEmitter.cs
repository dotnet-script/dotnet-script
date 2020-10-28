using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Dotnet.Script.Core
{
    public class ScriptEmitter
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly ScriptCompiler _scriptCompiler;

        public ScriptEmitter(ScriptConsole scriptConsole, ScriptCompiler scriptCompiler)
        {
            _scriptConsole = scriptConsole;
            _scriptCompiler = scriptCompiler;
        }

        public virtual ScriptEmitResult Emit<TReturn, THost>(ScriptContext context, string assemblyName)
        {
            var compilationContext = _scriptCompiler.CreateCompilationContext<TReturn, THost>(context);
            foreach (var warning in compilationContext.Warnings)
            {
                _scriptConsole.WriteHighlighted(warning.ToString());
            }

            if (compilationContext.Errors.Any())
            {
                foreach (var diagnostic in compilationContext.Errors)
                {
                    _scriptConsole.WriteError(diagnostic.ToString());
                }

                throw new CompilationErrorException("Script compilation failed due to one or more errors.", compilationContext.Errors.ToImmutableArray());
            }

            var compilation = compilationContext.Script.GetCompilation();
            compilation = compilation.WithAssemblyName(assemblyName);

            var peStream = new MemoryStream();
            EmitOptions emitOptions = null;

            if (context.OptimizationLevel == Microsoft.CodeAnalysis.OptimizationLevel.Debug)
            {
                emitOptions = new EmitOptions()
                    .WithDebugInformationFormat(DebugInformationFormat.Embedded);


            }

            var result = compilation.Emit(peStream, options: emitOptions);

            if (result.Success)
            {
                return new ScriptEmitResult(peStream, compilation.DirectiveReferences, compilationContext.RuntimeDependencies);
            }

            return ScriptEmitResult.Error(result.Diagnostics);
        }
    }
}
