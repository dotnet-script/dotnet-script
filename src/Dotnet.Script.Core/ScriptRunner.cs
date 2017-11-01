using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Text;

namespace Dotnet.Script.Core
{
    public class InteractiveRunner
    {
        private ScriptState<object> _scriptState;
        protected ScriptLogger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole Console;
        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, SourceCodeKind.Script);

        public InteractiveRunner(ScriptCompiler scriptCompiler, ScriptLogger logger, ScriptConsole console)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
            Console = console;
        }

        public virtual async Task RunLoop(string config, bool debugMode)
        {
            while (true)
            {
                Console.Out.Write("> ");

                var input = new StringBuilder();

                while (true)
                {
                    var line = Console.In.ReadLine();
                    input.AppendLine(line);

                    var syntaxTree = SyntaxFactory.ParseSyntaxTree(input.ToString(), ParseOptions);
                    if (!SyntaxFactory.IsCompleteSubmission(syntaxTree))
                    {
                        Console.Out.Write(". ");
                    }
                    else
                    {
                        break;
                    }
                }

                var sourceText = SourceText.From(input.ToString());
                var context = new ScriptContext(sourceText, Directory.GetCurrentDirectory(), config, Enumerable.Empty<string>(), debugMode: debugMode);
                await Execute(context);
            }
        }

        public virtual async Task Execute(ScriptContext context)
        {
            var globals = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);

            if (_scriptState == null)
            {
                var compilationContext = ScriptCompiler.CreateCompilationContext<object, InteractiveScriptGlobals>(context);
                _scriptState = await compilationContext.Script.RunAsync(globals, ex => true).ConfigureAwait(false);
            }
            else
            {
                _scriptState = await _scriptState.ContinueWithAsync(context.Code.ToString());
            }

            if (_scriptState.Exception != null)
            {
                Console.Error.Write(CSharpObjectFormatter.Instance.FormatException(_scriptState.Exception));
            }

            if (_scriptState.ReturnValue != null)
            {
                globals.Print(_scriptState.ReturnValue);
            }
        }
    }

    public class ScriptRunner
    {
        protected ScriptLogger Logger;
        protected ScriptCompiler ScriptCompiler;

        public ScriptRunner(ScriptCompiler scriptCompiler, ScriptLogger logger)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
        }

        public Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            var globals = new CommandLineScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);

            foreach (var arg in context.Args)
                globals.Args.Add(arg);

            return Execute<TReturn, CommandLineScriptGlobals>(context, globals);
        }

        public virtual Task<TReturn> Execute<TReturn, THost>(ScriptContext context, THost host)
        {
            var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);
            return Execute(compilationContext, host);
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
                Logger.Log("Script execution resulted in an exception.");
                Logger.Log(scriptState.Exception.Message);
                Logger.Log(scriptState.Exception.StackTrace);
                throw scriptState.Exception;
            }

            return scriptState.ReturnValue;
        }
    }
}
