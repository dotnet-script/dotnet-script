using System;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ScriptExecutionTests
    {
        private ScriptEnvironment _scriptEnvironment;

        public ScriptExecutionTests(ITestOutputHelper testOutputHelper)
        {
            var dllCache = Path.Combine(Path.GetTempPath(), "dotnet-scripts");
            FileUtils.RemoveDirectory(dllCache);
            testOutputHelper.Capture();
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldExecuteHelloWorld()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("HelloWorld");
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("InlineNugetPackage");
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldIncludeExceptionLineNumberAndFile()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Exception", "--nocache");
            Assert.Contains("Exception.csx:line 1", result.output);
        }

        [Fact]
        public void ShouldHandlePackageWithNativeLibraries()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("NativeLibrary");
            Assert.Contains("Connection successful", result.output);
        }

        [Fact]
        public void ShouldReturnExitCodeOneWhenScriptFails()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Exception");
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public void ShouldReturnStackTraceInformationWhenScriptFails()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Exception", "--nocache");
            Assert.Contains("die!", result.output);
            Assert.Contains("Exception.csx:line 1", result.output);
        }

        [Fact]
        public void ShouldReturnExitCodeOneWhenScriptFailsToCompile()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("CompilationError");
            Assert.Equal(1, result.exitCode);
        }

        [Fact]
        public void ShouldHandleIssue129()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue129");
            Assert.Contains("Bad HTTP authentication header", result.output);
        }

        [Fact]
        public void ShouldHandleIssue166()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue166", "--nocache");
            Assert.Contains("Connection successful", result.output);

        }

        [Fact]
        public void ShouldPassUnknownArgumentToScript()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Arguments", "arg1");
            Assert.Contains("arg1", result.output);
        }

        [Fact]
        public void ShouldPassKnownArgumentToScriptWhenEscapedByDoubleHyphen()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Arguments", "-- -v");
            Assert.Contains("-v", result.output);
        }

        [Fact]
        public void ShouldNotPassUnEscapedKnownArgumentToScript()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Arguments", "-v");
            Assert.DoesNotContain("-v", result.output);
        }

        [Fact]
        public void ShouldPropagateReturnValue()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("ReturnValue");
            Assert.Equal(42, result.exitCode);
        }

        [Fact]
        public void ShouldHandleIssue181()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue181");
            Assert.Contains("42", result.output);
        }

        [Fact]
        public void ShouldHandleIssue189()
        {
            var result = ScriptTestRunner.Default.Execute(Path.Combine(TestPathUtils.GetPathToTestFixtureFolder("Issue189"), "SomeFolder", "Script.csx"));
            Assert.Contains("Newtonsoft.Json.JsonConvert", result.output);
        }

        [Fact]
        public void ShouldHandleIssue198()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue198");
            Assert.Contains("NuGet.Client", result.output);
        }

        [Fact]
        public void ShouldHandleIssue204()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue204");
            Assert.Contains("System.Net.WebProxy", result.output);
        }

        [Fact]
        public void ShouldHandleIssue214()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue214");
            Assert.Contains("Hello World!", result.output);
        }

        [Fact]
        public void ShouldHandleIssue318()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue318");
            Assert.Contains("Hello World!", result.output);
        }

        [Theory]
        [InlineData("release","false")]
        [InlineData("debug","true")]
        public void ShouldCompileScriptWithReleaseConfiguration(string configuration, string expected)
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Configuration", $"-c {configuration}");
            Assert.Contains(expected, result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenSpecified()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Configuration", "-c debug");
            Assert.Contains("true", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenNotSpecified()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Configuration");
            Assert.Contains("true", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldHandleCSharp72()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("CSharp72");
            Assert.Contains("hi", result.output);
        }

        [Fact]
        public void ShouldEvaluateCode()
        {
            var code = "Console.WriteLine(12345);";
            var result = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("12345", result.output);
        }

        [Fact]
        public void ShouldEvaluateCodeInReleaseMode()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "TestFixtures", "Configuration", "Configuration.csx"));
            var result = ScriptTestRunner.Default.ExecuteCodeInReleaseMode(code);
            Assert.Contains("false", result.output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesinEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\"" using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var result = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesWithTrailingSemicoloninEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\""; using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var result = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldExecuteRemoteScript()
        {
            var url = "https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/HelloWorld.csx";
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteRemoteScriptUsingTinyUrl()
        {
            var url = "https://tinyurl.com/y8cda9zt";
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldHandleIssue268()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue268");
            Assert.Contains("value:", result.output);
        }

        [Fact]
        public void ShouldThrowExceptionOnInvalidMediaType()
        {
            var url = "https://github.com/filipw/dotnet-script/archive/0.20.0.zip";
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("not supported", result.output);
        }

        [Fact]
        public void ShouldHandleNonExistingRemoteScript()
        {
            var url = "https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/DoesNotExists.csx";
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("Not Found", result.output);
        }

        [Fact]
        public void ShouldHandleScriptUsingTheProcessClass()
        {
            // This test ensures that we can load the Process class.
            // This used to fail when executing a netcoreapp2.0 script
            // from dotnet-script built for netcoreapp2.1
            var result = ScriptTestRunner.Default.ExecuteFixture("Process");
            Assert.Contains("Success", result.output);
        }

        [Fact]
        public void ShouldHandleNuGetVersionRange()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("VersionRange");
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldHandleClearingNuGetCache()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("InlineNugetPackage","--nocache");
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
            TestPathUtils.RemovePackageFromGlobalNugetCache("automapper");
            result = ScriptTestRunner.Default.ExecuteFixture("InlineNugetPackage","--nocache");
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
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
    @"#r ""nuget: AgileObjects.AgileMapper, 0.25.0""
#r ""TestLibrary.dll""

    using AgileObjects.AgileMapper;

    IMapper mapper = Mapper.CreateNew();
    var testClass = new TestClass();
    Console.WriteLine(""Hello World!"");";


            using (var disposableFolder = new DisposableFolder())
            {
                var projectFolder = Path.Combine(disposableFolder.Path, "TestLibrary");
                ProcessHelper.RunAndCaptureOutput("dotnet", "new classlib -n TestLibrary", disposableFolder.Path);
                ProcessHelper.RunAndCaptureOutput("dotnet", "add TestLibrary.csproj package AgileObjects.AgileMapper -v 0.25.0", projectFolder);
                File.WriteAllText(Path.Combine(projectFolder, "Class1.cs"), code);
                File.WriteAllText(Path.Combine(projectFolder, "script.csx"), script);
                ProcessHelper.RunAndCaptureOutput("dotnet", "build -c release -o ./", projectFolder);

                var result = ScriptTestRunner.Default.Execute(Path.Combine(projectFolder, "script.csx"));

                Assert.Contains("Hello World!", result.output);
            }
        }
    }
}
