using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dotnet.Script.Core;
using Dotnet.Script.Core.Interactive.LineEditing;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.LanguageServices;
using Dotnet.Script.Shared.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ReplLanguageServicesTests
    {
        private static readonly ScriptOptions Options = CreateOptions();

        public ReplLanguageServicesTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        private static ScriptOptions CreateOptions()
        {
            var trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
            var references = trustedAssemblies
                .Split(Path.PathSeparator)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                // Held back so that ShouldCompleteAgainstAReferenceAddedMidSession has something to add.
                .Where(path => !Path.GetFileName(path).Equals("System.Text.Json.dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToArray();

            return ScriptOptions.Default
                .AddImports("System", "System.IO", "System.Collections.Generic", "System.Linq", "System.Text")
                .AddReferences(references);
        }

        private static ReplWorkspace CreateWorkspace(params string[] submissions)
        {
            var workspace = new ReplWorkspace();
            workspace.ScriptOptionsChanged(Options);

            foreach (var submission in submissions)
            {
                workspace.SubmissionExecuted(submission);
            }

            WarmUp(workspace);

            return workspace;
        }

        // The editor gives up on a cold compilation rather than blocking a key stroke, so assertions
        // would otherwise be racing the first compile.
        private static void WarmUp(ReplWorkspace workspace) =>
            workspace.WarmUpAsync(CancellationToken.None).GetAwaiter().GetResult();

        private static IReadOnlyList<string> Complete(ReplWorkspace workspace, string text)
        {
            var result = new RoslynCompletionProvider(workspace).GetCompletions(text, text.Length);

            // Null means Roslyn did not answer within the bounded wait, which is worth saying out loud
            // rather than failing later with a null reference.
            Assert.NotNull(result);

            return result.Items;
        }

        [Fact]
        public void ShouldCompleteMembersOfALocalFromAPreviousSubmission()
        {
            using var workspace = CreateWorkspace("var answer = 42;");

            Assert.Contains("ToString", Complete(workspace, "answer.ToStr"));
        }

        [Fact]
        public void ShouldCompleteExtensionMethods()
        {
            using var workspace = CreateWorkspace("var numbers = new List<int>();");

            // Extension methods are the headline reason for going through Roslyn rather than reflection.
            Assert.Contains("Where", Complete(workspace, "numbers.Whe"));
        }

        [Fact]
        public void ShouldCompleteAcrossSeveralSubmissions()
        {
            using var workspace = CreateWorkspace("var first = \"hello\";", "var second = first.Length;");

            Assert.Contains("CompareTo", Complete(workspace, "second.Compare"));
        }

        [Fact]
        public void ShouldKeepCompletingAsSubmissionsAreCommitted()
        {
            // Completing creates the scratch project, which has to be released again before the next
            // submission can reference the same head.
            using var workspace = CreateWorkspace();

            workspace.SubmissionExecuted("var first = \"hello\";");
            WarmUp(workspace);
            Assert.Contains("Length", Complete(workspace, "first.Len"));

            workspace.SubmissionExecuted("var second = 1;");
            WarmUp(workspace);
            Assert.Contains("CompareTo", Complete(workspace, "second.Compare"));

            workspace.SubmissionExecuted("var third = new List<int>();");
            WarmUp(workspace);
            Assert.Contains("Where", Complete(workspace, "third.Whe"));
        }

        [Fact]
        public void ShouldCompleteAgainstAReferenceAddedMidSession()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");

            Assert.Empty(Complete(workspace, "JsonSerializer.Ser"));

            // AddReferences(string) produces an UnresolvedMetadataReference, which is what #r yields and
            // what a workspace compilation refuses to accept unresolved.
            workspace.ScriptOptionsChanged(Options
                .AddReferences("System.Text.Json")
                .AddImports("System.Text.Json"));
            WarmUp(workspace);

            Assert.Contains("Serialize", Complete(workspace, "JsonSerializer.Ser"));
        }

        [Fact]
        public void ShouldKeepEarlierSubmissionsUsableAfterAReferenceIsAdded()
        {
            using var workspace = CreateWorkspace("var answer = 42;");

            workspace.ScriptOptionsChanged(Options
                .AddReferences("System.Text.Json")
                .AddImports("System.Text.Json"));
            WarmUp(workspace);

            Assert.Contains("ToString", Complete(workspace, "answer.ToStr"));
        }

        [Fact]
        public void ShouldMatchOnCamelCaseLikeAnIde()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");

            Assert.Contains("WriteLine", Complete(workspace, "Console.WL"));
        }

        [Fact]
        public void ShouldNarrowCompletionsToTheTypedPrefix()
        {
            using var workspace = CreateWorkspace("var text = \"hello\";");

            var items = Complete(workspace, "text.Sub");

            Assert.NotEmpty(items);
            Assert.All(items, item => Assert.StartsWith("Sub", item, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ShouldReplaceOnlyTheTypedIdentifier()
        {
            using var workspace = CreateWorkspace("var text = \"hello\";");
            const string input = "text.Sub";

            var result = new RoslynCompletionProvider(workspace).GetCompletions(input, input.Length);

            Assert.Equal(5, result.Start);
            Assert.Equal(3, result.Length);
        }

        [Fact]
        public void ShouldReturnNothingBeforeTheSessionHasStarted()
        {
            using var workspace = new ReplWorkspace();

            Assert.Null(new RoslynCompletionProvider(workspace).GetCompletions("Console.Wri", 11));
        }

        [Fact]
        public void ShouldForgetSubmissionsOnReset()
        {
            using var workspace = CreateWorkspace("var answer = 42;");
            workspace.Reset();
            workspace.ScriptOptionsChanged(Options);
            WarmUp(workspace);

            Assert.Empty(Complete(workspace, "answer.ToStr"));
        }

        [Fact]
        public void ShouldStopServingDocumentsOnceDisposed()
        {
            var workspace = CreateWorkspace("var answer = 42;");
            workspace.Dispose();

            Assert.Null(workspace.GetDocument("answer."));
            Assert.False(workspace.IsInitialized);
        }

        [Fact]
        public void ShouldClassifyTypesSemantically()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            var classifier = new RoslynClassifier(workspace, new SyntaxHighlighter());
            const string text = "var list = new List<int>();";

            var spans = classifier.Classify(text);

            Assert.Contains(spans, span => span.Kind == ClassificationKind.Type && span.Start == text.IndexOf("List", StringComparison.Ordinal));
        }

        [Fact]
        public void ShouldNotProduceOverlappingSpans()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            var classifier = new RoslynClassifier(workspace, new SyntaxHighlighter());

            var spans = classifier.Classify("Console.WriteLine(\"x\"); // trailing").OrderBy(span => span.Start).ToArray();

            for (var i = 1; i < spans.Length; i++)
            {
                Assert.True(spans[i].Start >= spans[i - 1].End);
            }
        }

        [Fact]
        public void ShouldFallBackToTheLexerWithoutAWorkspace()
        {
            using var workspace = new ReplWorkspace();
            var classifier = new RoslynClassifier(workspace, new SyntaxHighlighter());

            var spans = classifier.Classify("var x = 1;");

            Assert.Contains(spans, span => span.Kind == ClassificationKind.Keyword && span.Start == 0);
        }

        [Fact]
        public void ShouldDescribeTheMemberBeingInvoked()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            const string text = "Console.WriteLine(";

            var overloads = new RoslynQuickInfoProvider(workspace).GetQuickInfo(text, text.Length);

            Assert.Contains(overloads, signature => signature.Contains("Console.WriteLine(string"));
        }

        [Fact]
        public void ShouldListEveryOverloadOfTheMemberBeingInvoked()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            const string text = "Console.WriteLine(";

            var overloads = new RoslynQuickInfoProvider(workspace).GetQuickInfo(text, text.Length);

            Assert.True(overloads.Count > 1, $"expected several overloads, got {overloads.Count}");
            Assert.Equal(overloads.Count, overloads.Distinct().Count());
            Assert.Contains(overloads, signature => signature.Contains("()"));
        }

        [Fact]
        public void ShouldDescribeTheConstructorBeingInvoked()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            const string text = "new List<int>(";

            var overloads = new RoslynQuickInfoProvider(workspace).GetQuickInfo(text, text.Length);

            Assert.Contains(overloads, signature => signature.Contains("List"));
        }

        [Fact]
        public void ShouldDescribeASymbolOutsideAnArgumentList()
        {
            using var workspace = CreateWorkspace("var greeting = \"hello\";");
            const string text = "greeting";

            var info = new RoslynQuickInfoProvider(workspace).GetQuickInfo(text, text.Length);

            Assert.Contains(info, line => line.Contains("greeting"));
        }

        [Fact]
        public void ShouldReturnNoQuickInfoWithoutASession()
        {
            using var workspace = new ReplWorkspace();

            Assert.Empty(new RoslynQuickInfoProvider(workspace).GetQuickInfo("Console.WriteLine(", 18));
        }

        [Fact]
        public void ShouldCompleteThroughTheLineEditor()
        {
            using var workspace = CreateWorkspace("var greeting = \"hello\";");
            var device = new FakeConsoleDevice();
            var editor = new LineEditor(device, ReplColorScheme.Default, new ReplHistory())
            {
                CompletionProvider = new CompositeCompletionProvider(new RoslynCompletionProvider(workspace), new ReplCompletionProvider())
            };

            device.Enqueue(FakeConsoleDevice.Key.Type("greeting.ToUpp"));
            device.Enqueue(FakeConsoleDevice.Key.Press(ConsoleKey.Tab));
            device.Enqueue(FakeConsoleDevice.Key.Enter);

            Assert.Equal("greeting.ToUpper", editor.Read("> ", "* ", null));
        }

        [Fact]
        public void ShouldShowQuickInfoBelowTheInput()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");

            Assert.Contains("WriteLine", RenderQuickInfo(workspace, "Console.WriteLine("));
        }

        [Fact]
        public void ShouldCycleThroughOverloadsOnRepeatedCtrlT()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");

            var first = RenderQuickInfo(workspace, "Console.WriteLine(");
            var second = RenderQuickInfo(workspace, "Console.WriteLine(", cycles: 1);
            var third = RenderQuickInfo(workspace, "Console.WriteLine(", cycles: 2);

            Assert.StartsWith("[1/", first);
            Assert.StartsWith("[2/", second);
            Assert.StartsWith("[3/", third);
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void ShouldWrapAroundAfterTheLastOverload()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");
            var count = new RoslynQuickInfoProvider(workspace).GetQuickInfo("Console.WriteLine(", 18).Count;

            var first = RenderQuickInfo(workspace, "Console.WriteLine(");
            var wrapped = RenderQuickInfo(workspace, "Console.WriteLine(", cycles: count);

            Assert.Equal(first, wrapped);
        }

        [Fact]
        public void ShouldAlsoAcceptCtrlSpaceWhereTheTerminalDeliversIt()
        {
            using var workspace = CreateWorkspace("var ignored = 1;");

            var stepped = RenderQuickInfo(workspace, "Console.WriteLine(", cycles: 1, key: ConsoleKey.Spacebar);

            Assert.StartsWith("[2/", stepped);
        }

        /// <summary>
        /// Renders a session on a screen of its own and returns the info row. The stop sentinel leaves the
        /// frame mid-edit, which is the only point at which quick info is on screen.
        /// </summary>
        private static string RenderQuickInfo(ReplWorkspace workspace, string input, int cycles = 0, ConsoleKey key = ConsoleKey.T)
        {
            var device = new FakeConsoleDevice(width: 120);
            var editor = new LineEditor(device, ReplColorScheme.Default, new ReplHistory())
            {
                QuickInfoProvider = new RoslynQuickInfoProvider(workspace)
            };

            device.Enqueue(FakeConsoleDevice.Key.Type(input));

            for (var i = 0; i < cycles; i++)
            {
                device.Enqueue(FakeConsoleDevice.Key.Ctrl(key));
            }

            device.EnqueueStop();

            Assert.Throws<FakeConsoleDevice.StopReadingException>(() => editor.Read("> ", "* ", null));

            return device.GetRow(1);
        }

        [Fact]
        public void ShouldWorkWithTheScriptOptionsTheCompilerProduces()
        {
            const string code = "var answer = 42;";
            var compiler = new ScriptCompiler(TestOutputHelper.CreateTestLogFactory(), null, useRestoreCache: false);
            var context = new ScriptContext(
                SourceText.From(code),
                Directory.GetCurrentDirectory(),
                Enumerable.Empty<string>(),
                scriptMode: ScriptMode.REPL);

            // The compiler hands out references that still need resolving, which a workspace will not accept.
            var compilationContext = compiler.CreateCompilationContext<object, InteractiveScriptGlobals>(context);

            using var workspace = new ReplWorkspace();
            workspace.ScriptOptionsChanged(compilationContext.ScriptOptions);
            workspace.SubmissionExecuted(code);
            workspace.WarmUpAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Contains("ToString", Complete(workspace, "answer.ToStr"));
        }
    }
}
