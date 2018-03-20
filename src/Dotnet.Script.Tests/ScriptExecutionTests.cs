using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
            var result = ExecuteInProcess(Path.Combine("HelloWorld", "HelloWorld.csx"));
            //Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var result = ExecuteInProcess(Path.Combine("InlineNugetPackage", "InlineNugetPackage.csx"));
            //Assert.Contains("AutoMapper.MapperConfiguration", result.output);
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
        public static void ShouldReturnExitCodeOnenWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public static void ShouldReturnStackTraceInformationWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Contains("die!", result.output);
            Assert.Contains("Error.csx:line 1", result.output);
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

        [Fact]
        public static void ShouldHandleIssue181()
        {
            var result = Execute(Path.Combine("Issue181", "Issue181.csx"));
            Assert.Contains("42", result.output);
        }

        [Fact]
        public static void ShouldHandleIssue198()
        {
            var result = Execute(Path.Combine("Issue198", "Issue198.csx"));
            Assert.Contains("NuGet.Client", result.output);
        }


        [Fact]
        public static void ShouldHandleIssue204()
        {
            var result = Execute(Path.Combine("Issue204", "Issue204.csx"));
            Assert.Contains("System.Net.WebProxy", result.output);
        }

        [Fact]
        public static void ShouldHandleIssue214()
        {
            var result = Execute(Path.Combine("Issue214", "Issue214.csx"));
            Assert.Contains("Hello World!", result.output);
        }

        [Fact]
        public static void ShouldCompileScriptWithReleaseConfiguration()
        {
            var result = Execute(Path.Combine("Configuration", "Configuration.csx"),"-c", "release");
            Assert.Contains("false", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public static void ShouldCompileScriptWithDebugConfigurationWhenSpecified()
        {
            var result = Execute(Path.Combine("Configuration", "Configuration.csx"), "-c", "debug");
            Assert.Contains("true", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public static void ShouldCompileScriptWithDebugConfigurationWhenNotSpecified()
        {
            var result = Execute(Path.Combine("Configuration", "Configuration.csx"));
            Assert.Contains("true", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldHandleCSharp72()
        {
            var result = Execute(Path.Combine("CSharp72", "CSharp72.csx"));
            Assert.Contains("hi", result.output);
        }

        [Fact]
        public void ShouldEvaluateCode()
        {
            var code = "Console.WriteLine(12345);";
            var result = ExecuteCode(code);
            Assert.Contains("12345", result.output);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesinEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\"" using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var result = ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesWithTrailingSemicoloninEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\""; using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var result = ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public static void ShouldHandleIssue235()
        {
            string code =
            @"using AgileObjects.AgileMapper;
    public class TestClass
    {
        public TestClass()
        {
            IMapper mapper = Mapper.CreateNew();
        }
    }";

            string script =
            @"#! ""netcoreapp2.0""
    #r ""nuget: AgileObjects.AgileMapper, 0.23.1""
    #r ""TestLibrary.dll""
    
    using AgileObjects.AgileMapper;

    IMapper mapper = Mapper.CreateNew();
    var testClass = new TestClass();
    Console.WriteLine(""Hello World!"");";


            using (var disposableFolder = new DisposableFolder())
            {
                var projectFolder = Path.Combine(disposableFolder.Path, "TestLibrary");
                ProcessHelper.RunAndCaptureOutput("dotnet", new[] { "new classlib -n TestLibrary" }, disposableFolder.Path);
                ProcessHelper.RunAndCaptureOutput("dotnet", new[] { "add TestLibrary.csproj package AgileObjects.AgileMapper -v 0.23.0" }, projectFolder);
                File.WriteAllText(Path.Combine(projectFolder, "Class1.cs"), code);
                File.WriteAllText(Path.Combine(projectFolder, "script.csx"), script);
                ProcessHelper.RunAndCaptureOutput("dotnet", new[] { "build -c release -o ./" }, projectFolder);

                var dotnetScriptArguments = GetDotnetScriptArguments(Path.Combine(projectFolder, "script.csx"));
                var result = ProcessHelper.RunAndCaptureOutput("dotnet", dotnetScriptArguments);
                Assert.Contains("Hello World!", result.output);
            }
        }
        private static (string output, int exitCode) Execute(string fixture, params string[] arguments)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(Path.Combine("..", "..", "..", "TestFixtures", fixture), arguments));
            return result;
        }

        private static (string output, int exitCode) ExecuteCode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"eval", new[] { $"\"{code}\"" }));
            return result;
        }

        /// <summary>
        /// Use this method if you need to debug 
        /// </summary>        
        private static int ExecuteInProcess(string fixture, params string[] arguments)
        {
            var pathToFixture = Path.Combine("..", "..", "..", "TestFixtures", fixture);
            var allArguments = new List<string>(new[] { pathToFixture });
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
