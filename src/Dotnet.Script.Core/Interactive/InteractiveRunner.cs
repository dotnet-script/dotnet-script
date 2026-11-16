using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Script.Core.Interactive.LineEditing;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.NuGet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core
{
    public class InteractiveRunner
    {
        private const string Prompt = "> ";
        private const string ContinuationPrompt = "* ";

        private bool _shouldExit = false;
        private ScriptState<object> _scriptState;
        private ScriptOptions _scriptOptions;
        private readonly InteractiveScriptGlobals _globals;
        private readonly IReplLanguageService _languageService;
        protected Logger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole Console;
        private readonly string[] _packageSources;
        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview, kind: SourceCodeKind.Script);
        protected InteractiveCommandProvider InteractiveCommandParser = new InteractiveCommandProvider();
        protected string CurrentDirectory = Directory.GetCurrentDirectory();

        public InteractiveRunner(ScriptCompiler scriptCompiler, LogFactory logFactory, ScriptConsole console, string[] packageSources)
            : this(scriptCompiler, logFactory, console, packageSources, null)
        {
        }

        public InteractiveRunner(ScriptCompiler scriptCompiler, LogFactory logFactory, ScriptConsole console, string[] packageSources, IReplLanguageService languageService)
        {
            Logger = logFactory.CreateLogger<InteractiveRunner>();
            ScriptCompiler = scriptCompiler;
            Console = console;
            _packageSources = packageSources ?? Array.Empty<string>();
            _languageService = languageService;
            _globals = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);

            if (Console.LineEditor != null)
            {
                var builtIn = new ReplCompletionProvider(GetVariableNames, ResolveVariableType, () => CurrentDirectory);
                Console.LineEditor.CompletionProvider = new CompositeCompletionProvider(_languageService, builtIn);

                if (_languageService != null)
                {
                    Console.LineEditor.Classifier = _languageService;
                    Console.LineEditor.QuickInfoProvider = _languageService;
                }
            }
        }

        public virtual async Task RunLoop()
        {
            while (!_shouldExit)
            {
                var input = ReadInput();

                if (input == null)
                {
                    Exit();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (InteractiveCommandParser.TryProvideCommand(input, out var command))
                {
                    command.Execute(new CommandContext(Console, this));
                    continue;
                }

                await Execute(input);
            }
        }

        public virtual async Task RunLoopWithSeed(ScriptContext scriptContext)
        {
            await HandleScriptErrors(async () => await RunFirstScript(scriptContext));
            await RunLoop();
        }

        public virtual async Task<object> Execute(string input)
        {
            return await HandleScriptErrors(async () =>
            {
                if (_scriptState == null)
                {
                    var sourceText = SourceText.From(input);
                    var context = new ScriptContext(sourceText, CurrentDirectory, Enumerable.Empty<string>(),scriptMode: ScriptMode.REPL, packageSources: _packageSources);
                    await RunFirstScript(context);
                }
                else
                {
                    if (input.StartsWith("#r ") || input.StartsWith("#load "))
                    {
                        var lineRuntimeDependencies = ScriptCompiler.RuntimeDependencyResolver.GetDependenciesForCode(CurrentDirectory, ScriptMode.REPL,_packageSources, input).ToArray();
                        var lineDependencies = lineRuntimeDependencies.SelectMany(rtd => rtd.Assemblies).Distinct();

                        var scriptMap = lineRuntimeDependencies.ToDictionary(rdt => rdt.Name, rdt => rdt.Scripts);
                        if (scriptMap.Count > 0)
                        {
                            _scriptOptions =
                                _scriptOptions.WithSourceResolver(
                                    new NuGetSourceReferenceResolver(
                                        new SourceFileResolver(ImmutableArray<string>.Empty, CurrentDirectory), scriptMap));
                        }
                        foreach (var runtimeDependency in lineDependencies)
                        {
                            Logger.Debug("Adding reference to a runtime dependency => " + runtimeDependency);
                            _scriptOptions = _scriptOptions.AddReferences(MetadataReference.CreateFromFile(runtimeDependency.Path));
                        }

                        _languageService?.ScriptOptionsChanged(_scriptOptions);
                    }
                    _scriptState = await _scriptState.ContinueWithAsync(input, _scriptOptions, ex => true);
                    _languageService?.SubmissionExecuted(input);
                }
            });
        }

        public virtual void Reset()
        {
            _scriptState = null;
            _scriptOptions = null;
            _languageService?.Reset();
        }

        public virtual void Exit()
        {
            _shouldExit = true;
        }

        private async Task RunFirstScript(ScriptContext scriptContext)
        {
            foreach (var arg in scriptContext.Args)
            {
                _globals.Args.Add(arg);
            }

            var compilationContext = ScriptCompiler.CreateCompilationContext<object, InteractiveScriptGlobals>(scriptContext);
            Console.WriteDiagnostics(compilationContext.Warnings, compilationContext.Errors);

            if (compilationContext.Errors.Any())
            {
                throw new CompilationErrorException("Script compilation failed due to one or more errors.", compilationContext.Errors.ToImmutableArray());
            }

            _scriptState = await compilationContext.Script.RunAsync(_globals, ex => true).ConfigureAwait(false);
            _scriptOptions = compilationContext.ScriptOptions;

            _languageService?.ScriptOptionsChanged(_scriptOptions);
            _languageService?.SubmissionExecuted(scriptContext.Code.ToString());
        }

        private string ReadInput()
        {
            var editor = Console.LineEditor;
            return editor != null
                ? editor.Read(Prompt, ContinuationPrompt, IsCompleteSubmission)
                : ReadInputFromReader();
        }

        private string ReadInputFromReader()
        {
            Console.Out.Write(Prompt);

            var submission = new StringBuilder();
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null)
                {
                    return submission.Length == 0 ? null : submission.ToString();
                }

                if (submission.Length > 0)
                {
                    submission.Append('\n');
                }
                submission.Append(line);

                if (IsCompleteSubmission(submission.ToString()))
                {
                    return submission.ToString();
                }

                Console.Out.Write(ContinuationPrompt);
            }
        }

        private bool IsCompleteSubmission(string input) =>
            SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(input, ParseOptions));

        private IEnumerable<string> GetVariableNames() =>
            _scriptState?.Variables.Select(variable => variable.Name) ?? Enumerable.Empty<string>();

        private Type ResolveVariableType(string name) =>
            _scriptState?.Variables.FirstOrDefault(variable => variable.Name == name)?.Type;

        private async Task<object> HandleScriptErrors(Func<Task> doWork)
        {
            try
            {
                await doWork();
                if (_scriptState?.Exception != null)
                {
                    Console.WriteError(CSharpObjectFormatter.Instance.ToDisplayString(_scriptState.Exception));
                }

                if (_scriptState?.ReturnValue != null)
                {
                    _globals.Print(_scriptState.ReturnValue);
                }

                return _scriptState.ReturnValue;
            }
            catch (CompilationErrorException e)
            {
                foreach (var diagnostic in e.Diagnostics)
                {
                    Console.WriteError(diagnostic.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteError(CSharpObjectFormatter.Instance.ToDisplayString(e));
            }

            return null;
        }
    }
}
