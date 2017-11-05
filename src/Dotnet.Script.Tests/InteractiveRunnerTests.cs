using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Runtime;
using Dotnet.Script.DependencyModel.Logging;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Text;
using System;

namespace Dotnet.Script.Tests
{
    public class InteractiveRunnerTests
    {
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

        private InteractiveTestContext GetRunner(string[] commands)
        {
            var reader = new StringReader(string.Join(Environment.NewLine, commands));
            var writer = new StringWriter();
            var error = new StringWriter();

            var console = new ScriptConsole(writer, reader, error);
            var logger = new ScriptLogger(console.Error, true);
            var runtimeDependencyResolver = new RuntimeDependencyResolver(type => ((level, message) =>
            {
                if (level == LogLevel.Debug)
                {
                    logger.Verbose(message);
                }
                if (level == LogLevel.Info)
                {
                    logger.Log(message);
                }
            }));

            var compiler = new ScriptCompiler(logger, runtimeDependencyResolver);
            var runner = new InteractiveRunner(compiler, logger, console);
            return new InteractiveTestContext(console, runner);
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
            await ctx.Runner.RunLoop(false);

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
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
            await ctx.Runner.RunLoop(false);

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
            await ctx.Runner.RunLoop(false);

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
            await ctx.Runner.RunLoop(false);

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
            await ctx.Runner.RunLoop(false);

            var result = ctx.Console.Out.ToString();
            Assert.Contains("[AutoMapper.MapperConfiguration]", result);
        }

        [Fact]
        public async Task LoadedFile()
        {
            var pathToFixture = Path.Combine("..", "..", "..", "TestFixtures", "REPL", "main.csx");
            var commands = new[]
            {
                "var x = 5;",
                $@"#load ""{pathToFixture}""",
                "x * externalValue",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop(false);

            var result = ctx.Console.Out.ToString();
            Assert.Contains("500", result);
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
            await ctx.Runner.RunLoop(false);

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);

            var errResult = ctx.Console.Error.ToString();
            Assert.Contains("error CS0103: The name 'x' does not exist in the current context", errResult);
        }
    }
}
