using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.Shared.Tests;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptPublisherTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly CommandRunner _commandRunner;

        public ScriptPublisherTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
            _scriptEnvironment = ScriptEnvironment.Default;
            _commandRunner = new CommandRunner(GetLogFactory());
        }

        [Fact]
        public void SimplePublishTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);

                var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath}", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", _scriptEnvironment.RuntimeIdentifier, "main");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);

                var publishedFiles = Directory.EnumerateFiles(Path.Combine(workspaceFolder.Path, "publish", _scriptEnvironment.RuntimeIdentifier));
                if (_scriptEnvironment.NetCoreVersion.Major >= 3)
                    Assert.True(publishedFiles.Count() == 1, "There should be only a single published file");
                else
                    Assert.True(publishedFiles.Count() > 1, "There should be multiple published files");
            }
        }

        [Fact]
        public void SimplePublishTestToDifferentRuntimeId()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var runtimeId = _scriptEnvironment.RuntimeIdentifier == "win10-x64" ? "osx-x64" : "win10-x64";
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath} --runtime {runtimeId}");

                Assert.Equal(0, publishResult.exitCode);

                var publishPath = Path.Combine(workspaceFolder.Path, "publish", runtimeId);
                Assert.True(Directory.Exists(publishPath), $"Publish directory {publishPath} was not found.");

                using (var enumerator = Directory.EnumerateFiles(publishPath).GetEnumerator())
                {
                    Assert.True(enumerator.MoveNext(), $"Publish directory {publishPath} was empty.");
                }
            }
        }

        [Fact]
        public void SimplePublishToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            using (var publishRootFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath} -o {publishRootFolder.Path}");
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(publishRootFolder.Path, "main");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishFromCurrentDirectoryTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute("publish main.csx", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", _scriptEnvironment.RuntimeIdentifier, "main");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishFromCurrentDirectoryToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute("publish main.csx -o publish", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", "main");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishDllTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath} --dll", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine("publish", "main.dll");
                var dllRunResult = ScriptTestRunner.Default.Execute($"exec {dllPath}", workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void SimplePublishDllFromCurrentDirectoryTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute("publish main.csx --dll", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(workspaceFolder.Path, "publish", "main.dll");

                var dllRunResult = ScriptTestRunner.Default.Execute($"exec {dllPath}", workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void SimplePublishDllToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                using (var publishFolder = new DisposableFolder())
                {
                    var code = @"WriteLine(""hello world"");";
                    var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                    File.WriteAllText(mainPath, code);
                    var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath} --dll -o {publishFolder.Path}", workspaceFolder.Path);
                    Assert.Equal(0, publishResult.exitCode);

                    var dllPath = Path.Combine(publishFolder.Path, "main.dll");
                    var dllRunResult = ScriptTestRunner.Default.Execute($"exec {dllPath}", publishFolder.Path);

                    Assert.Equal(0, dllRunResult.exitCode);
                }
            }
        }

        [Fact]
        public void CustomDllNameTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var outputName = "testName";
                var assemblyName = $"{outputName}.dll";
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish main.csx --dll -n {outputName}", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(workspaceFolder.Path, "publish", assemblyName);
                var dllRunResult = ScriptTestRunner.Default.Execute($"exec {dllPath}", workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void CustomExeNameTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var exeName = "testName";
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish main.csx -o publish -n {exeName}", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", exeName);
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void DllWithArgsTests()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""Hello "" + Args[0] + Args[1] + Args[2] + Args[3] + Args[4]);";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishResult = ScriptTestRunner.Default.Execute($"publish {mainPath} --dll", workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(workspaceFolder.Path, "publish", "main.dll");
                var dllRunResult = ScriptTestRunner.Default.Execute($"exec {dllPath} -- w o r l d", workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
                Assert.Contains("Hello world", dllRunResult.output);
            }
        }

        [Fact]
        public void ShouldHandleReferencingAssemblyFromScriptFolder()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                ProcessHelper.RunAndCaptureOutput($"dotnet", $" new classlib -n MyCustomLibrary -o {workspaceFolder.Path}");
                ProcessHelper.RunAndCaptureOutput($"dotnet", $" build -o {workspaceFolder.Path}", workspaceFolder.Path);
                var code = $@"#r ""MyCustomLibrary.dll"" {Environment.NewLine} WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);

                var publishResult = ScriptTestRunner.Default.Execute("publish main.csx --dll --output .", workspaceFolder.Path);

                Assert.Equal(0, publishResult.exitCode);
            }
        }

        [Fact]
        public void ShouldHandleSpaceInPublishFolder()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);

                var publishResult = ScriptTestRunner.Default.Execute(@"publish main.csx -o ""publish folder""", workspaceFolder.Path);

                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish folder", "main");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        private LogFactory GetLogFactory()
        {
            return TestOutputHelper.CreateTestLogFactory();
        }
    }
}
