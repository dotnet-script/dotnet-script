using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
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

        [Fact]
        public void ShouldHandlePackageWithNativeLibraries()
        {
            var result = Execute(Path.Combine("NativeLibrary", "NativeLibrary.csx"));
            Assert.Contains("Connection successful", result);
        }

        private static string Execute(string fixture)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(Path.Combine("..", "..", "..", "TestFixtures", fixture)));
            return result;
        }

        /// <summary>
        /// Use this method if you need to debug 
        /// </summary>        
        private static int ExecuteInProcess(string fixture)
        {
            var pathToFixture = Path.Combine("..", "..", "..","TestFixtures", fixture);
            return Program.Main(new []{ pathToFixture });
        }

        private static string[] GetDotnetScriptArguments(string fixture)
        {
            return new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", "Debug", "netcoreapp1.1", "dotnet-script.dll"), fixture };
        }
    }
}
