using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dotnet.Script.DependencyModel.Environment;
using Xunit;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ScriptExecutionTests
    {
        private ScriptEnvironment _scriptEnvironment;

        public ScriptExecutionTests()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
        }

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
            if (_scriptEnvironment.IsWindows)
            {
                var result = Execute(Path.Combine("NativeLibrary", "NativeLibrary.csx"));
                Assert.Contains("Connection successful", result.output);
            }
        }

        [Fact]
        public void ShouldReturnExitCodeOnenWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public void ShouldReturnStackTraceInformationWhenScriptFails()
        {
            var result = Execute(Path.Combine("Exception", "Error.csx"));
            Assert.Contains("die!", result.output);
            Assert.Contains("Error.csx:line 1", result.output);
        }

        [Fact]
        public void ShouldReturnExitCodeOneWhenScriptFailsToCompile()
        {
            var result = Execute(Path.Combine("CompilationError", "CompilationError.csx"));
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public void ShouldHandleIssue129()
        {
            var result = Execute(Path.Combine("Issue129", "Issue129.csx"));
            Assert.Contains("Bad HTTP authentication header", result.output);
        }

        [Fact]
        public void ShouldHandleIssue166()
        {
            // System.Data.SqlClient loads native assets
            // No story on *nix yet.
            if (_scriptEnvironment.IsWindows)
            {
                var result = Execute(Path.Combine("Issue166", "Issue166.csx"));
                Assert.Contains("Connection successful", result.output);
            }
        }

        [Fact]
        public void ShouldPassUnknownArgumentToScript()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "arg1");
            Assert.Contains("arg1", result.output);
        }

        [Fact]
        public void ShouldPassKnownArgumentToScriptWhenEscapedByDoubleHyphen()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "--", "-v");
            Assert.Contains("-v", result.output);
        }

        [Fact]
        public void ShouldNotPassUnEscapedKnownArgumentToScript()
        {
            var result = Execute($"{Path.Combine("Arguments", "Arguments.csx")}", "-v");
            Assert.DoesNotContain("-v", result.output);
        }

        [Fact]
        public void ShouldPropagateReturnValue()
        {
            var result = Execute($"{Path.Combine("ReturnValue", "ReturnValue.csx")}");
            Assert.Equal(42,result.exitCode);
        }

        [Fact]
        public void ShouldHandleIssue181()
        {
            var result = Execute(Path.Combine("Issue181", "Issue181.csx"));
            Assert.Contains("42", result.output);
        }

        [Fact]
        public void ShouldHandleIssue189()
        {
            var result = Execute(Path.Combine("Issue189","SomeFolder","Script.csx"));
            Assert.Contains("Newtonsoft.Json.JsonConvert", result.output);
        }

        [Fact]
        public void ShouldHandleIssue198()
        {
            var result = Execute(Path.Combine("Issue198", "Issue198.csx"));
            Assert.Contains("NuGet.Client", result.output);
        }

        [Fact]
        public void ShouldHandleIssue204()
        {
            var result = Execute(Path.Combine("Issue204", "Issue204.csx"));
            Assert.Contains("System.Net.WebProxy", result.output);
        }

        [Fact]
        public void ShouldHandleIssue214()
        {
            var result = Execute(Path.Combine("Issue214", "Issue214.csx"));
            Assert.Contains("Hello World!", result.output);
        }

        [Fact]
        public void ShouldCompileScriptWithReleaseConfiguration()
        {
            var result = Execute(Path.Combine("Configuration", "Configuration.csx"),"-c", "release");
            Assert.Contains("false", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenSpecified()
        {
            var result = Execute(Path.Combine("Configuration", "Configuration.csx"), "-c", "debug");
            Assert.Contains("true", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenNotSpecified()
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
        public void ShouldEvaluateCodeInReleaseMode()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "TestFixtures", "Configuration", "Configuration.csx"));
            var result = ExecuteCodeInReleaseMode(code);
            Assert.Contains("false", result.output, StringComparison.OrdinalIgnoreCase);
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
        public void ShouldExecuteRemoteScript()
        {
            var url = "https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/HelloWorld.csx";
            Program.Main(new[] {url });
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(url));
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteRemoteScriptUsingTinyUrl()
        {
            var url = "https://tinyurl.com/y8cda9zt";
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(url));
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldHandleIssue268()
        {
            var result = Execute($"{Path.Combine("Issue268", "Issue268.csx")}");
            Assert.Contains("value:", result.output);
        }

        [Fact]
        public void ShouldThrowExceptionOnInvalidMediaType()
        {
            var t = ResolveTargetFramework();
            var url = "https://github.com/filipw/dotnet-script/archive/0.20.0.zip";
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(url));
            Assert.Contains("not supported", result.output);
        }

        [Fact]
        public void ShouldHandleNonExistingRemoteScript()
        {
            var url = "https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/DoesNotExists.csx";
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(url));
            Assert.Contains("Not Found", result.output);
        }

        [Fact]
        public void ShouldHandleScriptUsingTheProcessClass()
        {
            // This test ensures that we can load the Process class.
            // This used to fail when executing a netcoreapp2.0 script
            // from dotnet-script built for netcoreapp2.1
            var result = Execute(Path.Combine("Process", "Process.csx"));
            Assert.Contains("Success", result.output);
        }


        [Fact(Skip = "This also failes when run a standard netcoreapp2.1 console app")]
        public void ShouldHandleIssue235()
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
    @"#r ""nuget: AgileObjects.AgileMapper, 0.23.1""
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
        private (string output, int exitCode) Execute(string fixture, params string[] arguments)
        {            
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(Path.Combine("..", "..", "..", "TestFixtures", fixture), arguments));
            return result;
        }
        
        private (string output, int exitCode) ExecuteCode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"eval", new[] { $"\"{code}\"" }));
            return result;
        }

        private (string output, int exitCode) ExecuteCodeInReleaseMode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"-c", new[] {"release", "eval", $"\"{code}\"" }));
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

        /// <summary>
        /// Use this method if you need to debug 
        /// </summary>        
        private static int ExecuteCodeProcess(string code, params string[] arguments)
        {            
            var allArguments = new List<string>();
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            allArguments.Add("eval");
            allArguments.Add(code);
            return Program.Main(allArguments.ToArray());
        }

        private string[] GetDotnetScriptArguments(string fixture, params string[] arguments)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            var allArguments = new List<string>(new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, _scriptEnvironment.TargetFramework, "dotnet-script.dll"), fixture });
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            return allArguments.ToArray();
        }

        private static string ResolveTargetFramework()
        {
            return Assembly.GetEntryAssembly().GetCustomAttributes()
                .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                .Select(x => x.FrameworkName)
                .FirstOrDefault();
        }
}
}
