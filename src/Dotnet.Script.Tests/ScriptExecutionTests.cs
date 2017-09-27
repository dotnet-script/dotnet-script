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
            var result = ExecuteInProcess(Path.Combine("HelloWorld", "HelloWorld.csx"));
            //Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var result = Execute(Path.Combine("InlineNugetPackage", "InlineNugetPackage.csx"));
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldIncludeExceptionLineNumberAndFile()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Contains("Error.csx:line 1", result.output);
        }

        [Fact]
        public void ShouldHandlePackageWithNativeLibraries()
        {
            //ExecuteInProcess(Path.Combine("NativeLibrary", "NativeLibrary.csx"));
            var result = Execute(Path.Combine("NativeLibrary", "NativeLibrary.csx"));
            Assert.Contains("Connection successful", result.output);
        }
        
        [Fact]
        public static void ShouldReturnNonZeroExitCodeWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.NotEqual(0, result.exitCode);
        }

        [Fact]
        public static void ShouldHandleIssue129()
        {
            //ExecuteInProcess(Path.Combine("Issue129", "Issue129.csx"));
            var result = Execute(Path.Combine("Issue129", "Issue129.csx"));
            Assert.Contains("Bad HTTP authentication header", result.output);
        }

        private static (string output, int exitCode) Execute(string fixture)
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
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            return new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, "netcoreapp2.0", "dotnet-script.dll"), fixture };
        }
    }
}
