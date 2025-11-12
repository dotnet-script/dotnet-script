using System;
using System.IO;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ScriptDependencyContextReaderTests
    {
        public ScriptDependencyContextReaderTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldThrowMeaningfulExceptionWhenRuntimeTargetIsMissing()
        {
            using var projectFolder = new DisposableFolder();
            var pathToAssetsFile = Path.Combine(projectFolder.Path, "SampleLibrary", "obj", "project.assets.json");
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", "new console -n SampleLibrary", projectFolder.Path);
            //Assert.Equal(0, result.ExitCode);
            //result = ProcessHelper.RunAndCaptureOutput("dotnet", "restore", projectFolder.Path);
            //Assert.Equal(0, result.ExitCode);

            if (!File.Exists(pathToAssetsFile))
            {
                throw new InvalidOperationException($"Expected assets file to be present at '{pathToAssetsFile}' but it was not found. Command output: {result.Output}");
            }

            TestOutputHelper.Current.TestOutputHelper.WriteLine($"Path to assets file: {pathToAssetsFile}");
            var dependencyResolver = new ScriptDependencyContextReader(TestOutputHelper.CreateTestLogFactory());

            var exception = Assert.Throws<InvalidOperationException>(() => dependencyResolver.ReadDependencyContext(pathToAssetsFile));
            Assert.Contains("Make sure that the project file was restored using a RID (runtime identifier).", exception.Message);
        }

        [Fact]
        public void ShouldThrowMeaningfulExceptionWhenPassingAnInvalidAssetsFile()
        {
            var pathToAssetsFile = Path.Combine(Path.GetTempPath(), "project.assets.json");
            var dependencyResolver = new ScriptDependencyContextReader(TestOutputHelper.CreateTestLogFactory());
            var exception = Assert.Throws<InvalidOperationException>(() => dependencyResolver.ReadDependencyContext(pathToAssetsFile));
            Assert.Contains("Make sure that the file exists and that it is a valid 'project.assets.json' file.", exception.Message);
        }
    }
}