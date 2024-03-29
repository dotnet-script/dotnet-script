﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private bool _shouldExit = false;
        private ScriptState<object> _scriptState;
        private ScriptOptions _scriptOptions;
        private readonly InteractiveScriptGlobals _globals;
        protected Logger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole Console;
        private readonly string[] _packageSources;
        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview, kind: SourceCodeKind.Script);
        protected InteractiveCommandProvider InteractiveCommandParser = new InteractiveCommandProvider();
        protected string CurrentDirectory = Directory.GetCurrentDirectory();

        public InteractiveRunner(ScriptCompiler scriptCompiler, LogFactory logFactory, ScriptConsole console, string[] packageSources)
        {
            Logger = logFactory.CreateLogger<InteractiveRunner>();
            ScriptCompiler = scriptCompiler;
            Console = console;
            _packageSources = packageSources ?? Array.Empty<string>();
            _globals = new InteractiveScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);
        }

        public virtual async Task RunLoop()
        {
            while (!_shouldExit)
            {
                Console.Out.Write("> ");
                var input = ReadInput();

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
                    }
                    _scriptState = await _scriptState.ContinueWithAsync(input, _scriptOptions, ex => true);
                }
            });
        }

        public virtual void Reset()
        {
            _scriptState = null;
            _scriptOptions = null;
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
        }

        private string ReadInput()
        {
            var input = new StringBuilder();

            while (true)
            {
                var line = Console.ReadLine();
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
