using System.Collections.Generic;
using System.IO;
using Dotnet.Script.DependencyModel.Environment;
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
            Assert.Contains("Hello World", result.output);
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
            // We have no story for this on *nix yet
            if (RuntimeHelper.IsWindows())
            {
                var result = Execute(Path.Combine("NativeLibrary", "NativeLibrary.csx"));
                Assert.Contains("Connection successful", result.output);
            }            
        }
        
        [Fact]
        public static void ShouldReturnExitCodeOneWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public static void ShouldReturnExitCodeOneWhenScriptFailsToCompile()
        {
            var result = Execute(Path.Combine("CompilationError", "CompilationError.csx"));
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public static void ShouldHandleIssue129()
        {
            var result = Execute(Path.Combine("Issue129", "Issue129.csx"));
            Assert.Contains("Bad HTTP authentication header", result.output);
        }

        [Fact]
        public static void ShouldHandleIssue166()
        {
            // System.Data.SqlClient loads native assets
            // No story on *nix yet.
            if (RuntimeHelper.IsWindows())
            {
                var result = Execute(Path.Combine("Issue166", "Issue166.csx"));
                Assert.Contains("Connection successful", result.output);
            }                
        }

        [Fact]
        public static void ShouldPassUnknownArgumentToScript()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "arg1");
            Assert.Contains("arg1", result.output);
        }

        [Fact]
        public static void ShouldPassKnownArgumentToScriptWhenEscapedByDoubleHyphen()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "--", "-v");
            Assert.Contains("-v", result.output);
        }

        [Fact]
        public static void ShouldNotPassUnEscapedKnownArgumentToScript()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "-v");
            Assert.DoesNotContain("-v", result.output);            
        }

        [Fact]
        public static void ShouldPropagateReturnValue()
        {
            var result = Execute($"{Path.Combine("ReturnValue", "ReturnValue.csx")}");
            Assert.Equal(42,result.exitCode);
        }



        private static (string output, int exitCode) Execute(string fixture, params string[] arguments)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(Path.Combine("..", "..", "..", "TestFixtures", fixture), arguments));
            return result;
        }

        /// <summary>
        /// Use this method if you need to debug 
        /// </summary>        
        private static int ExecuteInProcess(string fixture, params string[] arguments)
        {
            var pathToFixture = Path.Combine("..", "..", "..","TestFixtures", fixture);
            var allArguments = new List<string>(new[] {pathToFixture});
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            return Program.Main(allArguments.ToArray());
        }

        private static string[] GetDotnetScriptArguments(string fixture, params string[] arguments)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            var allArguments = new List<string>(new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, "netcoreapp2.0", "dotnet-script.dll"), fixture });
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            return allArguments.ToArray();
        }
    }
}
