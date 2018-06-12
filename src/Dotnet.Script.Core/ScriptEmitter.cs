using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.IO;

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

        public virtual ScriptEmitResult Emit<TReturn>(ScriptContext context, string assemblyName = null)
        {
            try
            {
                var compilationContext = _scriptCompiler.CreateCompilationContext<TReturn, CommandLineScriptGlobals>(context);

                var compilation = compilationContext.Script.GetCompilation();
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    var compilationOptions = compilationContext.Script.GetCompilation().Options
                        .WithScriptClassName(assemblyName);
                    compilation = compilationContext.Script.GetCompilation()
                        .WithOptions(compilationOptions)
                        .WithAssemblyName(assemblyName);
                }


                var peStream = new MemoryStream();
                var pdbStream = new MemoryStream();
                var result = compilation.Emit(peStream, pdbStream: pdbStream, options: new EmitOptions().
                    WithDebugInformationFormat(DebugInformationFormat.PortablePdb));

                if (result.Success)
                {
                    return new ScriptEmitResult(peStream, pdbStream, compilation.DirectiveReferences);
                }

                return ScriptEmitResult.Error(result.Diagnostics);
            }
            catch (CompilationErrorException e)
            {
                foreach (var diagnostic in e.Diagnostics)
                {
                    _scriptConsole.WritePrettyError(diagnostic.ToString());
                }

                throw;
            }
        }
    }
}
