using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Shared.Tests
{
    public abstract class InteractiveRunnerTestsBase
    {
        public InteractiveRunnerTestsBase(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        private class InteractiveTestContext
        {
            public InteractiveTestContext(ScriptConsole console, InteractiveRunner runner)
            {
                Console = console;
                Runner = runner;
            }

            public ScriptConsole Console { get; }
            public InteractiveRunner Runner { get; }
        }

        private InteractiveTestContext GetRunner(params string[] commands)
        {
            var reader = new StringReader(string.Join(Environment.NewLine, commands));
            var writer = new StringWriter();
            var error = new StringWriter();

            var console = new ScriptConsole(writer, reader, error);

            var logFactory = TestOutputHelper.CreateTestLogFactory();
            var runtimeDependencyResolver = new RuntimeDependencyResolver(logFactory, useRestoreCache: false);

            var compiler = new ScriptCompiler(logFactory, runtimeDependencyResolver);
            var runner = new InteractiveRunner(compiler, logFactory, console, Array.Empty<string>());
            return new InteractiveTestContext(console, runner);
        }

        [Fact]
        public async Task ReturnValue()
        {
            var ctx = GetRunner();
            await ctx.Runner.Execute("1+1");
            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
        }

        [Fact]
        public async Task SimpleOutput()
        {
            var commands = new[]
            {
                "var x = 1;",
                "x+x",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
        }

        [Fact]
        public async Task RuntimeException()
        {
            var commands = new[]
            {
                "foo",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Error.ToString();
            Assert.Contains("(1,1): error CS0103: The name 'foo' does not exist in the current context", result);
        }

        [Fact]
        public async Task ValueFromSeededFile()
        {
            var commands = new[]
            {
                "x+x",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoopWithSeed(new ScriptContext(SourceText.From(@"var x = 1;"), Directory.GetCurrentDirectory(), new string[0], scriptMode: ScriptMode.REPL));

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
        }

        [Fact]
        public async Task RuntimeExceptionFromSeededFile()
        {
            var commands = new[]
            {
                "var x = 1;",
                "x+x",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoopWithSeed(new ScriptContext(SourceText.From(@"throw new Exception(""die!"");"), Directory.GetCurrentDirectory(), new string[0], scriptMode: ScriptMode.REPL));

            var errorResult = ctx.Console.Error.ToString();
            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
            Assert.Contains("die!", errorResult);
        }

        [Fact]
        public async Task Multiline()
        {
            var commands = new[]
            {
                "class Foo {",
                "}",
                "var x = new Foo();",
                "x",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("Submission#0.Foo", result);
        }

        [Fact]
        public async Task ExtensionMethod()
        {
            var commands = new[]
            {
                @"var x = ""foo"";",
                @"static string SayHi(this string txt) { return $""hi, {txt}""; }",
                "x.SayHi()",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("hi, foo", result);
        }

        [Fact]
        public async Task GlobalsObject()
        {
            var commands = new[]
            {
                @"var x = ""foo"";",
                "Print(x);",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("foo", result);
        }

        [Fact]
        public async Task NugetPackageReference()
        {
            var commands = new[]
            {
                "var x = 1;",
                @"#r ""nuget: Automapper, 6.1.1""",
                "using AutoMapper;",
                "typeof(MapperConfiguration)",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("[AutoMapper.MapperConfiguration]", result);
        }

        [Fact]
        public async Task ScriptPackageReference()
        {
            var commands = new[]
            {
                "var x = 1;",
                @"#load ""nuget: simple-targets-csx, 6.0.0""",
                "using static SimpleTargets;",
                "typeof(TargetDictionary)",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();
            var result = ctx.Console.Out.ToString();
            Assert.Contains("[Submission#1+SimpleTargets+TargetDictionary]", result);
        }

        [Fact]
        public async Task LoadedFile()
        {
            var pathToFixture = Path.Combine("..", "..", "..", "..", "Dotnet.Script.Tests", "TestFixtures", "REPL", "main.csx");
            var commands = new[]
            {
                "var x = 5;",
                $@"#load ""{pathToFixture}""",
                "x * externalValue",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("500", result);
        }

        [Fact]
        public async Task LoadFileWithNuGetReference()
        {
            var pathToFixture = Path.Combine("..", "..", "..", "..", "Dotnet.Script.Tests", "TestFixtures", "InlineNugetPackage", "InlineNugetPackage.csx");
            var commands = new[]
            {
                $@"#load ""{pathToFixture}""",
                "using AutoMapper;",
                "typeof(MapperConfiguration)",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("AutoMapper.MapperConfiguration", result);
        }


        [Fact]
        public async Task ResetCommand()
        {
            var commands = new[]
            {
                "var x = 1;",
                "x+x",
                "#reset",
                "x",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);

            var errResult = ctx.Console.Error.ToString();
            Assert.Contains("error CS0103: The name 'x' does not exist in the current context", errResult);
        }

        [Fact]
        public async Task NugetPackageReferenceAsTheFirstLine()
        {
            var commands = new[]
            {
                @"#r ""nuget: Automapper, 6.1.1""",
                "using AutoMapper;",
                "typeof(MapperConfiguration)",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();
            Assert.Contains("[AutoMapper.MapperConfiguration]", result);
        }

        [Fact]
        public async Task ScriptPackageReferenceAsTheFirstLine()
        {
            var commands = new[]
            {
                @"#load ""nuget: simple-targets-csx, 6.0.0""",
                "using static SimpleTargets;",
                "typeof(TargetDictionary)",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();
            var result = ctx.Console.Out.ToString();
            Assert.Contains("[Submission#0+SimpleTargets+TargetDictionary]", result);
        }
    }
}
