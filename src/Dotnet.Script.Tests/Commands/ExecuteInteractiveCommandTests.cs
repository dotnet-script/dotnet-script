using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.Core.Commands;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests.Commands
{
    public class ExecuteInteractiveCommandTests
    {
        public ExecuteInteractiveCommandTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        private (ExecuteInteractiveCommand Command, ScriptConsole Console) GetExecuteInteractiveCommand(string[] commands)
        {
            var reader = new StringReader(string.Join(Environment.NewLine, commands));
            var writer = new StringWriter();
            var error = new StringWriter();

            var console = new ScriptConsole(writer, reader, error);
            var logFactory = TestOutputHelper.CreateTestLogFactory();
            return (new ExecuteInteractiveCommand(console, logFactory), console);
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

            var ctx = GetExecuteInteractiveCommand(commands);
            await ctx.Command.Execute(new ExecuteInteractiveCommandOptions(null, null, null));

            var result = ctx.Console.Out.ToString();
            Assert.Contains("2", result);
        }

        [Fact]
        public async Task SeedFromScript()
        {
            var pathToFixture = Path.Combine("..", "..", "..", "..", "Dotnet.Script.Tests", "TestFixtures", "REPL", "main.csx");
            var commands = new[]
            {
                "var x = 5;",
                "x * externalValue",
                "#exit"
            };

            var ctx = GetExecuteInteractiveCommand(commands);
            await ctx.Command.Execute(new ExecuteInteractiveCommandOptions(new ScriptFile(pathToFixture), null, null));

            var result = ctx.Console.Out.ToString();
            Assert.Contains("500", result);
        }
    }
}
