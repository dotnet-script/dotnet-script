using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScriptExecutionTests
    {
        [Fact]
        public void ShouldExecuteHelloWorld()
        {
            var result = Execute(Path.Combine("HelloWorld", "HelloWorld.csx"));
            Assert.Contains("Hello World", result);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var result = Execute(Path.Combine("InlineNugetPackage", "InlineNugetPackage.csx"));
            Assert.Contains("AutoMapper.MapperConfiguration", result);
        }

        [Fact]
        public void ShouldIncludeExceptionLineNumberAndFile()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Contains("Error.csx:line 1", result);
        }

        private static string Execute(string fixture)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(Path.Combine("..", "..", "..", "TestFixtures", fixture)));
            return result;
        }

        private static string[] GetDotnetScriptArguments(string fixture)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            return new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, "netcoreapp1.1", "dotnet-script.dll"), fixture };
        }
    }
}
