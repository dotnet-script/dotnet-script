using System.IO;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ProjectFileTests
    {
        [Fact]
        public void ShouldParseProjectFile()
        {
            using(var projectFolder = new DisposableFolder())
            {
                var projectFile = new ProjectFile();
                var pathToProjectFile = Path.Combine(projectFolder.Path, "project.csproj");
                projectFile.PackageReferences.Add(new PackageReference("SomePackage","1.2.3"));
                projectFile.PackageReferences.Add(new PackageReference("AnotherPackage","3.2.1"));
                projectFile.Save(Path.Combine(projectFolder.Path, "project.csproj"));

                var parsedProjectFile = new ProjectFile(File.ReadAllText(pathToProjectFile));

                Assert.Contains(new PackageReference("SomePackage", "1.2.3"), parsedProjectFile.PackageReferences);
                Assert.Contains(new PackageReference("AnotherPackage", "3.2.1"), parsedProjectFile.PackageReferences);
            }
        }
    }
}