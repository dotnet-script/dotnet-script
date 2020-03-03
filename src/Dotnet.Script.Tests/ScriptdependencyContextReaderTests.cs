using System;
using System.IO;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptDependencyContextReaderTests
    {
        public ScriptDependencyContextReaderTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldThrowMeaningfulExceptionWhenRuntimeTargetIsMissing()
        {
            using (var projectFolder = new DisposableFolder())
            {
                ProcessHelper.RunAndCaptureOutput("dotnet", "new console -n SampleLibrary", projectFolder.Path);
                ProcessHelper.RunAndCaptureOutput("dotnet", "restore", projectFolder.Path);
                var pathToAssetsFile = Path.Combine(projectFolder.Path, "SampleLibrary" ,"obj","project.assets.json");
                var dependencyResolver = new ScriptDependencyContextReader(TestOutputHelper.CreateTestLogFactory());

                var exception = Assert.Throws<InvalidOperationException>(() => dependencyResolver.ReadDependencyContext(pathToAssetsFile));
                Assert.Contains("Make sure that the project file was restored using a RID (runtime identifier).", exception.Message);
            }
        }

        [Fact]
        public void ShouldThrowMeaningfulExceptionWhenPassingAnInvalidAssetsFile()
        {
            var pathToAssetsFile = Path.Combine(Path.GetTempPath(),"project.assets.json");
            var dependencyResolver = new ScriptDependencyContextReader(TestOutputHelper.CreateTestLogFactory());
            var exception = Assert.Throws<InvalidOperationException>(() => dependencyResolver.ReadDependencyContext(pathToAssetsFile));
            Assert.Contains("Make sure that the file exists and that it is a valid 'project.assets.json' file.", exception.Message);
        }
    }
}