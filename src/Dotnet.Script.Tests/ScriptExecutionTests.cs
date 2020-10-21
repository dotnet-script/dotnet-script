using System;
using System.IO;
using System.Text;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ScriptExecutionTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;

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
            var result = ScriptTestRunner.Default.ExecuteFixture("HelloWorld", "--no-cache");
            Assert.Contains("Hello World", result.output);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("InlineNugetPackage");
            Assert.Contains("AutoMapper.MapperConfiguration", result.output);
        }

        [Fact]
        public void ShouldHandleNullableContextAsError()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Nullable");
            Assert.Equal(1, result.exitCode);
            Assert.Contains("error CS8625", result.output);
        }

        [Fact]
        public void ShouldNotHandleDisabledNullableContextAsError()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("NullableDisabled");
            Assert.Equal(0, result.exitCode);
        }

        [Fact]
        public void ShouldIncludeExceptionLineNumberAndFile()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Exception", "--no-cache");
            Assert.Contains("Exception.csx:line 1", result.output);
        }

        [Fact]
        public void ShouldHandlePackageWithNativeLibraries()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("NativeLibrary", "--no-cache");
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
            var result = ScriptTestRunner.Default.ExecuteFixture("Exception", "--no-cache");
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
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue166", "--no-cache");
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
            var result = ScriptTestRunner.Default.ExecuteFixtureInProcess("Issue198");
            // Assert.Contains("NuGet.Client", result.output);
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
        [InlineData("release", "false")]
        [InlineData("debug", "true")]
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
        public void ShouldHandleCSharp80()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("CSharp80");
            Assert.Equal(0, result.exitCode);

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

        [Theory]
        [InlineData("https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/HelloWorld.csx",
                    "Hello World")]
        [InlineData("https://github.com/filipw/dotnet-script/files/5035247/hello.csx.gz",
                    "Hello, world!")]
        public void ShouldExecuteRemoteScript(string url, string output)
        {
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains(output, result.output);
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
        public void ShouldHandleIssue435()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("Issue435");
            Assert.Contains("value:Microsoft.Extensions.Configuration.ConfigurationBuilder", result.output);
        }

        [Fact]
        public void ShouldLoadMicrosoftExtensionsDependencyInjection()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("MicrosoftExtensionsDependencyInjection");
            Assert.Contains("Microsoft.Extensions.DependencyInjection.IServiceCollection", result.output);
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
        public void ShouldThrowMeaningfulErrorMessageWhenDependencyIsNotFound()
        {
            using (var libraryFolder = new DisposableFolder())
            {
                // Create a package that we can reference

                ProcessHelper.RunAndCaptureOutput("dotnet", "new classlib -n SampleLibrary", libraryFolder.Path);
                ProcessHelper.RunAndCaptureOutput("dotnet", "pack", libraryFolder.Path);

                using (var scriptFolder = new DisposableFolder())
                {
                    var code = new StringBuilder();
                    code.AppendLine("#r \"nuget:SampleLibrary, 1.0.0\"");
                    code.AppendLine("WriteLine(42)");
                    var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");
                    File.WriteAllText(pathToScript, code.ToString());

                    // Run once to ensure that it is cached.
                    var result = ScriptTestRunner.Default.Execute(pathToScript);
                    Assert.Contains("42", result.output);

                    // Remove the package from the global NuGet cache
                    TestPathUtils.RemovePackageFromGlobalNugetCache("SampleLibrary");

                    //ScriptTestRunner.Default.ExecuteInProcess(pathToScript);

                    result = ScriptTestRunner.Default.Execute(pathToScript);
                    Assert.Contains("Try executing/publishing the script", result.output);

                    // Run again with the '--no-cache' option to assert that the advice actually worked.
                    result = ScriptTestRunner.Default.Execute($"{pathToScript} --no-cache");
                    Assert.Contains("42", result.output);
                }
            }
        }

        [Fact]
        public void ShouldHandleIssue235()
        {
            string code =
            @"using AgileObjects.AgileMapper;
    namespace TestLibrary
    {
        public class TestClass
        {
            public TestClass()
            {
                IMapper mapper = Mapper.CreateNew();
            }
        }
    }
    ";

            string script =
    @"#r ""nuget: AgileObjects.AgileMapper, 0.25.0""
#r ""testlib/TestLibrary.dll""

    using AgileObjects.AgileMapper;
    using TestLibrary;

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
                ProcessHelper.RunAndCaptureOutput("dotnet", "build -c release -o testlib", projectFolder);

                var result = ScriptTestRunner.Default.Execute(Path.Combine(projectFolder, "script.csx"));

                Assert.Contains("Hello World!", result.output);
            }
        }


        [Fact]
        public void ShouldHandleLocalNuGetConfigWithRelativePath()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using (var packageLibraryFolder = new DisposableFolder())
            {
                CreateTestPackage(packageLibraryFolder.Path);

                string pathToScriptFile = CreateTestScript(packageLibraryFolder.Path);

                var result = ScriptTestRunner.Default.Execute(pathToScriptFile);
                Assert.Contains("Success", result.output);
            }
        }

        [Fact]
        public void ShouldHandleLocalNuGetConfigWithRelativePathInParentFolder()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using (var packageLibraryFolder = new DisposableFolder())
            {
                CreateTestPackage(packageLibraryFolder.Path);

                var scriptFolder = Path.Combine(packageLibraryFolder.Path, "ScriptFolder");
                Directory.CreateDirectory(scriptFolder);
                string pathToScriptFile = CreateTestScript(scriptFolder);

                var result = ScriptTestRunner.Default.Execute(pathToScriptFile);
                Assert.Contains("Success", result.output);
            }
        }

        [Fact]
        public void ShouldHandleLocalNuGetFileWhenPathContainsSpace()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using (var packageLibraryFolder = new DisposableFolder())
            {
                var packageLibraryFolderPath = Path.Combine(packageLibraryFolder.Path, "library folder");
                Directory.CreateDirectory(packageLibraryFolderPath);

                CreateTestPackage(packageLibraryFolderPath);

                string pathToScriptFile = CreateTestScript(packageLibraryFolderPath);

                var result = ScriptTestRunner.Default.Execute($"\"{pathToScriptFile}\"");
                Assert.Contains("Success", result.output);
            }
        }

        [Fact]
        public void ShouldHandleScriptWithTargetFrameworkInShebang()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture("TargetFrameworkInShebang");
            Assert.Contains("Hello world!", result.output);
        }

        [Fact]
        public void ShouldIgnoreGlobalJsonInScriptFolder()
        {
            var fixture = "InvalidGlobalJson";
            var workingDirectory = Path.GetDirectoryName(TestPathUtils.GetPathToTestFixture(fixture));
            var result = ScriptTestRunner.Default.ExecuteFixture("InvalidGlobalJson", $"--no-cache", workingDirectory);
            Assert.Contains("Hello world!", result.output);
        }


        private static string CreateTestScript(string scriptFolder)
        {
            string script = @"
#r ""nuget:NuGetConfigTestLibrary, 1.0.0""
WriteLine(""Success"");
                ";
            string pathToScriptFile = Path.Combine(scriptFolder, "testscript.csx");
            File.WriteAllText(pathToScriptFile, script);
            return pathToScriptFile;
        }

        private static void CreateTestPackage(string packageLibraryFolder)
        {
            ProcessHelper.RunAndCaptureOutput("dotnet", "new classlib -n NuGetConfigTestLibrary -f netstandard2.0", packageLibraryFolder);
            var projectFolder = Path.Combine(packageLibraryFolder, "NuGetConfigTestLibrary");
            ProcessHelper.RunAndCaptureOutput("dotnet", $"pack -o \"{Path.Combine(packageLibraryFolder, "packagePath")}\"", projectFolder);
            CreateNuGetConfig(packageLibraryFolder);
        }

        private static void CreateNuGetConfig(string packageLibraryFolder)
        {
            string nugetConfig = @"
<configuration>
    <packageSources>
        <clear/>
        <add key=""localSource"" value=""packagePath""/>
    </packageSources>
></configuration>
                ";
            File.WriteAllText(Path.Combine(packageLibraryFolder, "NuGet.Config"), nugetConfig);
        }
    }
}
