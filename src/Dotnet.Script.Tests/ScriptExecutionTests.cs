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
        public ScriptExecutionTests(ITestOutputHelper testOutputHelper)
        {
            var dllCache = Path.Combine(Path.GetTempPath(), "dotnet-scripts");
            FileUtils.RemoveDirectory(dllCache);
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldExecuteHelloWorld()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("HelloWorld", "--no-cache");
            Assert.Contains("Hello World", output);
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("InlineNugetPackage");
            Assert.Contains("AutoMapper.MapperConfiguration", output);
        }

        [Fact]
        public void ShouldHandleNullableContextAsError()
        {
            var (output, exitCode) = ScriptTestRunner.Default.ExecuteFixture("Nullable");
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS8625", output);
        }

        [Fact]
        public void ShouldNotHandleDisabledNullableContextAsError()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("NullableDisabled");
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ShouldIncludeExceptionLineNumberAndFile()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Exception", "--no-cache");
            Assert.Contains("Exception.csx:line 1", output);
        }

        [Fact]
        public void ShouldHandlePackageWithNativeLibraries()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("NativeLibrary", "--no-cache");
            Assert.Contains("Connection successful", output);
        }

        [Fact]
        public void ShouldReturnExitCodeOneWhenScriptFails()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("Exception");
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void ShouldReturnStackTraceInformationWhenScriptFails()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Exception", "--no-cache");
            Assert.Contains("die!", output);
            Assert.Contains("Exception.csx:line 1", output);
        }

        [Fact]
        public void ShouldReturnExitCodeOneWhenScriptFailsToCompile()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("CompilationError");
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void ShouldWriteCompilerWarningsToStandardError()
        {
            var result = ScriptTestRunner.Default.ExecuteFixture(fixture: "CompilationWarning", "--no-cache");
            Assert.True(string.IsNullOrWhiteSpace(result.StandardOut));
            Assert.Contains("CS1998", result.StandardError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldHandleIssue129()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue129");
            Assert.Contains("Bad HTTP authentication header", output);
        }

        [Fact]
        public void ShouldHandleIssue166()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue166", "--no-cache");
            Assert.Contains("Connection successful", output);

        }

        [Fact]
        public void ShouldPassUnknownArgumentToScript()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Arguments", "arg1");
            Assert.Contains("arg1", output);
        }

        [Fact]
        public void ShouldPassKnownArgumentToScriptWhenEscapedByDoubleHyphen()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Arguments", "-- -v");
            Assert.Contains("-v", output);
        }

        [Fact]
        public void ShouldNotPassUnEscapedKnownArgumentToScript()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Arguments", "-v");
            Assert.DoesNotContain("-v", output);
        }

        [Fact]
        public void ShouldPropagateReturnValue()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("ReturnValue");
            Assert.Equal(42, exitCode);
        }

        [Fact]
        public void ShouldHandleIssue181()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue181");
            Assert.Contains("42", output);
        }

        [Fact]
        public void ShouldHandleIssue189()
        {
            var (output, _) = ScriptTestRunner.Default.Execute($"\"{Path.Combine(TestPathUtils.GetPathToTestFixtureFolder("Issue189"), "SomeFolder", "Script.csx")}\"");
            Assert.Contains("Newtonsoft.Json.JsonConvert", output);
        }

        [Fact]
        public void ShouldHandleIssue198()
        {
            var result = ScriptTestRunner.ExecuteFixtureInProcess("Issue198");
            // Assert.Contains("NuGet.Client", result.output);
        }

        [Fact]
        public void ShouldHandleIssue204()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue204");
            Assert.Contains("System.Net.WebProxy", output);
        }

        [Fact]
        public void ShouldHandleIssue214()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue214");
            Assert.Contains("Hello World!", output);
        }

        [Fact]
        public void ShouldHandleIssue318()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue318");
            Assert.Contains("Hello World!", output);
        }

        [Theory]
        [InlineData("release", "false")]
        [InlineData("debug", "true")]
        public void ShouldCompileScriptWithReleaseConfiguration(string configuration, string expected)
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Configuration", $"-c {configuration}");
            Assert.Contains(expected, output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenSpecified()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Configuration", "-c debug");
            Assert.Contains("true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldCompileScriptWithDebugConfigurationWhenNotSpecified()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Configuration");
            Assert.Contains("true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldHandleCSharp72()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("CSharp72");
            Assert.Contains("hi", output);
        }

        [Fact]
        public void ShouldHandleCSharp80()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("CSharp80");
            Assert.Equal(0, exitCode);

        }

        [Fact]
        public void ShouldEvaluateCode()
        {
            var code = "Console.WriteLine(12345);";
            var (output, _) = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("12345", output);
        }

        [Fact]
        public void ShouldEvaluateCodeInReleaseMode()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "TestFixtures", "Configuration", "Configuration.csx"));
            var (output, _) = ScriptTestRunner.Default.ExecuteCodeInReleaseMode(code);
            Assert.Contains("false", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesinEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\"" using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var (output, _) = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", output);
        }

        [Fact]
        public void ShouldSupportInlineNugetReferencesWithTrailingSemicoloninEvaluatedCode()
        {
            var code = @"#r \""nuget: AutoMapper, 6.1.1\""; using AutoMapper; Console.WriteLine(typeof(MapperConfiguration));";
            var (output, _) = ScriptTestRunner.Default.ExecuteCode(code);
            Assert.Contains("AutoMapper.MapperConfiguration", output);
        }

        [Theory]
        [InlineData("https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/HelloWorld.csx",
                    "Hello World")]
        [InlineData("https://github.com/dotnet-script/dotnet-script/files/5035247/hello.csx.gz",
                    "Hello, world!")]
        public void ShouldExecuteRemoteScript(string url, string output)
        {
            var result = ScriptTestRunner.Default.Execute(url);
            Assert.Contains(output, result.Output);
        }

        [Fact]
        public void ShouldExecuteRemoteScriptUsingTinyUrl()
        {
            var url = "https://tinyurl.com/y8cda9zt";
            var (output, _) = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("Hello World", output);
        }

        [Fact]
        public void ShouldHandleIssue268()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue268");
            Assert.Contains("value:", output);
        }

        [Fact]
        public void ShouldHandleIssue435()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Issue435");
            Assert.Contains("value:Microsoft.Extensions.Configuration.ConfigurationBuilder", output);
        }

        [Fact]
        public void ShouldLoadMicrosoftExtensionsDependencyInjection()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("MicrosoftExtensionsDependencyInjection");
            Assert.Contains("Microsoft.Extensions.DependencyInjection.IServiceCollection", output);
        }

        [Fact]
        public void ShouldThrowExceptionOnInvalidMediaType()
        {
            var url = "https://github.com/dotnet-script/dotnet-script/archive/0.20.0.zip";
            var (output, _) = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("not supported", output);
        }

        [Fact]
        public void ShouldHandleNonExistingRemoteScript()
        {
            var url = "https://gist.githubusercontent.com/seesharper/5d6859509ea8364a1fdf66bbf5b7923d/raw/0a32bac2c3ea807f9379a38e251d93e39c8131cb/DoesNotExists.csx";
            var (output, _) = ScriptTestRunner.Default.Execute(url);
            Assert.Contains("Not Found", output);
        }

        [Fact]
        public void ShouldHandleScriptUsingTheProcessClass()
        {
            // This test ensures that we can load the Process class.
            // This used to fail when executing a netcoreapp2.0 script
            // from dotnet-script built for netcoreapp2.1
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Process");
            Assert.Contains("Success", output);
        }

        [Fact]
        public void ShouldHandleNuGetVersionRange()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("VersionRange");
            Assert.Contains("AutoMapper.MapperConfiguration", output);
        }

        [Fact]
        public void ShouldThrowMeaningfulErrorMessageWhenDependencyIsNotFound()
        {
            using var libraryFolder = new DisposableFolder();
            // Create a package that we can reference

            ProcessHelper.RunAndCaptureOutput("dotnet", "new classlib -n SampleLibrary", libraryFolder.Path);
            ProcessHelper.RunAndCaptureOutput("dotnet", "pack", libraryFolder.Path);

            using var scriptFolder = new DisposableFolder();
            var code = new StringBuilder();
            code.AppendLine("#r \"nuget:SampleLibrary, 1.0.0\"");
            code.AppendLine("WriteLine(42)");
            var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");
            File.WriteAllText(pathToScript, code.ToString());

            // Run once to ensure that it is cached.
            var result = ScriptTestRunner.Default.Execute(pathToScript);
            Assert.Contains("42", result.Output);

            // Remove the package from the global NuGet cache
            TestPathUtils.RemovePackageFromGlobalNugetCache("SampleLibrary");

            //ScriptTestRunner.Default.ExecuteInProcess(pathToScript);

            result = ScriptTestRunner.Default.Execute(pathToScript);
            Assert.Contains("Try executing/publishing the script", result.Output);

            // Run again with the '--no-cache' option to assert that the advice actually worked.
            result = ScriptTestRunner.Default.Execute($"{pathToScript} --no-cache");
            Assert.Contains("42", result.Output);
        }

        [Fact]
        public void ShouldHandleIssue613()
        {
            var (_, exitCode) = ScriptTestRunner.Default.ExecuteFixture("Issue613");
            Assert.Equal(0, exitCode);
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


            using var disposableFolder = new DisposableFolder();
            var projectFolder = Path.Combine(disposableFolder.Path, "TestLibrary");
            ProcessHelper.RunAndCaptureOutput("dotnet", "new classlib -n TestLibrary", disposableFolder.Path);
            ProcessHelper.RunAndCaptureOutput("dotnet", "add TestLibrary.csproj package AgileObjects.AgileMapper -v 0.25.0", projectFolder);
            File.WriteAllText(Path.Combine(projectFolder, "Class1.cs"), code);
            File.WriteAllText(Path.Combine(projectFolder, "script.csx"), script);
            ProcessHelper.RunAndCaptureOutput("dotnet", "build -c release -o testlib", projectFolder);

            var (output, exitCode) = ScriptTestRunner.Default.Execute(Path.Combine(projectFolder, "script.csx"));

            Assert.Contains("Hello World!", output);
        }


        [Fact]
        public void ShouldHandleLocalNuGetConfigWithRelativePath()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using var packageLibraryFolder = new DisposableFolder();
            CreateTestPackage(packageLibraryFolder.Path);

            string pathToScriptFile = CreateTestScript(packageLibraryFolder.Path);

            var (output, exitCode) = ScriptTestRunner.Default.Execute(pathToScriptFile);
            Assert.Contains("Success", output);
        }

        [Fact]
        public void ShouldHandleLocalNuGetConfigWithRelativePathInParentFolder()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using var packageLibraryFolder = new DisposableFolder();
            CreateTestPackage(packageLibraryFolder.Path);

            var scriptFolder = Path.Combine(packageLibraryFolder.Path, "ScriptFolder");
            Directory.CreateDirectory(scriptFolder);
            string pathToScriptFile = CreateTestScript(scriptFolder);

            var (output, exitCode) = ScriptTestRunner.Default.Execute(pathToScriptFile);
            Assert.Contains("Success", output);
        }

        [Fact]
        public void ShouldHandleLocalNuGetFileWhenPathContainsSpace()
        {
            TestPathUtils.RemovePackageFromGlobalNugetCache("NuGetConfigTestLibrary");

            using var packageLibraryFolder = new DisposableFolder();
            var packageLibraryFolderPath = Path.Combine(packageLibraryFolder.Path, "library folder");
            Directory.CreateDirectory(packageLibraryFolderPath);

            CreateTestPackage(packageLibraryFolderPath);

            string pathToScriptFile = CreateTestScript(packageLibraryFolderPath);

            var (output, exitCode) = ScriptTestRunner.Default.Execute($"\"{pathToScriptFile}\"");
            Assert.Contains("Success", output);
        }

        [Fact]
        public void ShouldHandleScriptWithTargetFrameworkInShebang()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("TargetFrameworkInShebang");
            Assert.Contains("Hello world!", output);
        }

        [Fact]
        public void ShouldIgnoreGlobalJsonInScriptFolder()
        {
            var fixture = "InvalidGlobalJson";
            var workingDirectory = Path.GetDirectoryName(TestPathUtils.GetPathToTestFixture(fixture));
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("InvalidGlobalJson", $"--no-cache", workingDirectory);
            Assert.Contains("Hello world!", output);
        }

        [Fact]
        public void ShouldIsolateScriptAssemblies()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("Isolation", "--isolated-load-context");
            Assert.Contains("2.0.0.0", output);
        }

        [Fact]
        public void ShouldSetCurrentContextualReflectionContext()
        {
            var (output, _) = ScriptTestRunner.Default.ExecuteFixture("CurrentContextualReflectionContext", "--isolated-load-context");
            Assert.Contains("Dotnet.Script.Core.ScriptAssemblyLoadContext", output);
        }

        [Fact]
        public void ShouldCompileAndExecuteWithWebSdk()
        {
            var processResult = ScriptTestRunner.Default.ExecuteFixture("WebApi", "--no-cache");
            Assert.Equal(0, processResult.ExitCode);
        }

        [Fact]
        public void ShouldThrowExceptionWhenSdkIsNotSupported()
        {
            var processResult = ScriptTestRunner.Default.ExecuteFixture("UnsupportedSdk", "--no-cache");
            Assert.StartsWith("The sdk 'Unsupported' is not supported", processResult.StandardError);
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
