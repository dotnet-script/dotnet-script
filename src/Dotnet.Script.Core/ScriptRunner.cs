using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script.Core
{
    public class ScriptRunner
    {
        protected ScriptLogger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole ScriptConsole;

        public ScriptRunner(ScriptCompiler scriptCompiler, ScriptLogger logger, ScriptConsole scriptConsole)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
            ScriptConsole = scriptConsole;
        }

        public Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            var globals = new CommandLineScriptGlobals(ScriptConsole.Out, CSharpObjectFormatter.Instance);

            foreach (var arg in context.Args)
                globals.Args.Add(arg);

            return Execute<TReturn, CommandLineScriptGlobals>(context, globals);
        }

        public virtual Task<TReturn> Execute<TReturn, THost>(ScriptContext context, THost host)
        {
            try
            {
                var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);
                return Execute(compilationContext, host);
            }
            catch (CompilationErrorException e)
            {
                foreach (var diagnostic in e.Diagnostics)
                {
                    ScriptConsole.WritePrettyError(diagnostic.ToString());
                }

                throw;
            }
        }

        public virtual async Task<TReturn> Execute<TReturn, THost>(ScriptCompilationContext<TReturn> compilationContext, THost host)
        {
            var scriptResult = await compilationContext.Script.RunAsync(host, ex => true).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        protected TReturn ProcessScriptState<TReturn>(ScriptState<TReturn> scriptState)
        {
            if (scriptState.Exception != null)
            {
                // once Roslyn ships with this, we can format he exception using CSharpObjectFormatter
                // https://github.com/dotnet/roslyn/blob/4175350b87f928e136cbb14c2668b7cb3338d5a1/src/Scripting/Core/Hosting/CommonMemberFilter.cs#L18
                ScriptConsole.WritePrettyError(scriptState.Exception.ToString());
                throw new ScriptRuntimeException("Script execution resulted in an exception.", scriptState.Exception);
            }

            return scriptState.ReturnValue;
        }
    }
}
