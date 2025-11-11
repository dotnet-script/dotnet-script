using System.IO;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ProjectFileTests
    {
        [Fact]
        public void ShouldParseProjectFile()
        {
            using var projectFolder = new DisposableFolder();
            var projectFile = new ProjectFile();
            var pathToProjectFile = Path.Combine(projectFolder.Path, "project.csproj");
            projectFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));
            projectFile.PackageReferences.Add(new PackageReference("AnotherPackage", "3.2.1"));
            projectFile.Save(Path.Combine(projectFolder.Path, "project.csproj"));

            var parsedProjectFile = new ProjectFile(File.ReadAllText(pathToProjectFile));

            Assert.Contains(new PackageReference("SomePackage", "1.2.3"), parsedProjectFile.PackageReferences);
            Assert.Contains(new PackageReference("AnotherPackage", "3.2.1"), parsedProjectFile.PackageReferences);
        }

        [Fact]
        public void ShouldBeEqualWhenPackagesAreEqual()
        {
            var firstFile = new ProjectFile();
            firstFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));
            firstFile.PackageReferences.Add(new PackageReference("AnotherPackage", "3.2.1"));

            var secondFile = new ProjectFile();
            secondFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));
            secondFile.PackageReferences.Add(new PackageReference("AnotherPackage", "3.2.1"));

            Assert.Equal(firstFile, secondFile);
        }

        [Fact]
        public void ShouldNotBeEqualWhenPackagesAreDifferent()
        {
            var firstFile = new ProjectFile();
            firstFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));
            firstFile.PackageReferences.Add(new PackageReference("AnotherPackage", "3.2.1"));

            var secondFile = new ProjectFile();
            secondFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));

            Assert.NotEqual(firstFile, secondFile);
        }

        [Fact]
        public void ShouldBeEqualWhenReferencesAreEqual()
        {
            var firstFile = new ProjectFile();
            firstFile.AssemblyReferences.Add(new AssemblyReference("somePath"));
            firstFile.AssemblyReferences.Add(new AssemblyReference("anotherPath"));

            var secondFile = new ProjectFile();
            secondFile.AssemblyReferences.Add(new AssemblyReference("somePath"));
            secondFile.AssemblyReferences.Add(new AssemblyReference("anotherPath"));

            Assert.Equal(firstFile, secondFile);
        }

        [Fact]
        public void ShouldNotBeEqualWhenReferencesAreDifferent()
        {
            var firstFile = new ProjectFile();
            firstFile.AssemblyReferences.Add(new AssemblyReference("somePath"));
            firstFile.AssemblyReferences.Add(new AssemblyReference("anotherPath"));

            var secondFile = new ProjectFile();
            secondFile.AssemblyReferences.Add(new AssemblyReference("somePath"));

            Assert.NotEqual(firstFile, secondFile);
        }

        [Fact]
        public void ShouldBeCacheableWhenPackagesArePinned()
        {
            var projectFile = new ProjectFile();
            projectFile.PackageReferences.Add(new PackageReference("SomePackage", "1.2.3"));
            projectFile.PackageReferences.Add(new PackageReference("AnotherPackage", "3.2.1"));

        }
    }
}