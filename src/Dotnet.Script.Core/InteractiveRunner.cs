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
using System;

namespace Dotnet.Script.Core
{
    public class InteractiveRunner
    {
        private ScriptState<object> _scriptState;
        private ScriptOptions _scriptOptions;

        protected ScriptLogger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole Console;
        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);

        public InteractiveRunner(ScriptCompiler scriptCompiler, ScriptLogger logger, ScriptConsole console)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
            Console = console;
        }

        public virtual async Task RunLoop(string config, bool debugMode)
        {
            var directory = Directory.GetCurrentDirectory();
            while (true)
            {
                Console.Out.Write("> ");
                var input = ReadInput();

                var sourceText = SourceText.From(input);
                var context = new ScriptContext(sourceText, directory, config, Enumerable.Empty<string>(), debugMode: debugMode);
                await Execute(context);
            }
        }

        private string ReadInput()
        {
            var input = new StringBuilder();

            while (true)
            {
                var line = Console.In.ReadLine();
                input.AppendLine(line);

                var syntaxTree = SyntaxFactory.ParseSyntaxTree(input.ToString(), ParseOptions);
                if (!SyntaxFactory.IsCompleteSubmission(syntaxTree))
                {
                    Console.Out.Write("* ");
                }
                else
                {
                    break;
                }
            }

            return input.ToString();
        }

        public virtual async Task Execute(ScriptContext context)
        {
            var globals = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);

            if (_scriptState == null)
            {
                var compilationContext = ScriptCompiler.CreateCompilationContext<object, InteractiveScriptGlobals>(context);
                _scriptState = await compilationContext.Script.RunAsync(globals, ex => true).ConfigureAwait(false);
                _scriptOptions = compilationContext.ScriptOptions;
            }
            else
            {
                var code = context.Code.ToString();
                var lineDependencies = ScriptCompiler.RuntimeDependencyResolver.GetDependenciesFromCode(context.WorkingDirectory, code);

                foreach (var runtimeDependency in lineDependencies)
                {
                    Logger.Verbose("Adding reference to a runtime dependency => " + runtimeDependency);
                    _scriptOptions = _scriptOptions.AddReferences(MetadataReference.CreateFromFile(runtimeDependency.Path));
                }

                try
                {
                    _scriptState = await _scriptState.ContinueWithAsync(code, _scriptOptions, ex => true);
                }
                catch (Exception e)
                {
                    Console.Error.Write(CSharpObjectFormatter.Instance.FormatException(e));
                }
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
}
