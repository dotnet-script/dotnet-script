using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.Shared.Tests;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class DotnetRestorerTests
    {
        private PackageReference ValidPackageReferenceA => new PackageReference("Newtonsoft.Json", "12.0.3");
        private PackageReference ValidPackageReferenceB => new PackageReference("Moq", "4.14.5");

        private PackageReference InvalidPackageReferenceA => new PackageReference("7c63e1f5-2248-ed31-9480-e4cb5ac322fe", "1.0.0");

        public DotnetRestorerTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldRestoreProjectPackageReferences()
        {
            using (var projectFolder = new DisposableFolder())
            {
                var pathToProjectFile = Path.Combine(projectFolder.Path, "script.csproj");

                var projectFile = new ProjectFile();
                projectFile.PackageReferences.Add(ValidPackageReferenceA);
                projectFile.PackageReferences.Add(ValidPackageReferenceB);
                projectFile.Save(pathToProjectFile);

                var projectFileInfo = new ProjectFileInfo(pathToProjectFile, string.Empty);

                var logFactory = TestOutputHelper.CreateTestLogFactory();
                var commandRunner = new CommandRunner(logFactory);
                var restorer = new DotnetRestorer(commandRunner, logFactory);

                var pathToProjectObjDirectory = Path.Combine(projectFolder.Path, "obj");

                Assert.False(Directory.Exists(pathToProjectObjDirectory));

                restorer.Restore(projectFileInfo, Array.Empty<string>());

                Assert.True(Directory.Exists(pathToProjectObjDirectory));
            }
        }

        [Fact]
        public void ShouldThrowExceptionOnRestoreError()
        {
            using (var projectFolder = new DisposableFolder())
            {
                var pathToProjectFile = Path.Combine(projectFolder.Path, "script.csproj");

                var projectFile = new ProjectFile();
                projectFile.PackageReferences.Add(ValidPackageReferenceA);
                projectFile.PackageReferences.Add(InvalidPackageReferenceA);
                projectFile.PackageReferences.Add(ValidPackageReferenceB);
                projectFile.Save(pathToProjectFile);

                var projectFileInfo = new ProjectFileInfo(pathToProjectFile, string.Empty);

                var logFactory = TestOutputHelper.CreateTestLogFactory();
                var commandRunner = new CommandRunner(logFactory);
                var restorer = new DotnetRestorer(commandRunner, logFactory);

                var exception = Assert.Throws<Exception>(() =>
                {
                    restorer.Restore(projectFileInfo, Array.Empty<string>());
                });

                Assert.Contains("NU1101", exception.Message); // unable to find package 
            }
        }
    }
}
