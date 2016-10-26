using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script
{
    public class ScriptRunner
    {
        protected readonly ScriptLogger Logger;
        protected readonly ScriptCompiler ScriptCompiler;

        public ScriptRunner(ScriptCompiler scriptCompiler, ScriptLogger logger)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
        }

        public virtual async Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, InteractiveScriptGlobals>(context);
            var globals = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);

            foreach (var arg in context.Args)
            {
                globals.Args.Add(arg);
            }

            var scriptResult = await compilationContext.Script.RunAsync(globals).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        public virtual async Task<TReturn> Execute<TReturn, THost>(ScriptContext context, THost host)
        {
            var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);

            var scriptResult = await compilationContext.Script.RunAsync(host).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        protected TReturn ProcessScriptState<TReturn>(ScriptState<TReturn> scriptState)
        {
            if (scriptState.Exception != null)
            {
                Logger.Log("Script execution resulted in an exception.");
                Logger.Log(scriptState.Exception.Message);
                Logger.Log(scriptState.Exception.StackTrace);
            }

            return scriptState.ReturnValue;
        }
    }
}