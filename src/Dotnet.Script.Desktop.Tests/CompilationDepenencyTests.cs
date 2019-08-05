using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Desktop.Tests
{
    public class CompilationDependencyTests
    {
        public CompilationDependencyTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Theory]
        [InlineData("netcoreapp2.1")]
        [InlineData("netcoreapp3.0")]
        public void ShouldGetCompilationDependenciesForNetCoreApp2_1(string targetFramework)
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("HelloWorld");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, targetFramework);
            Assert.True(dependencies.Count() > 0);
        }

        private CompilationDependencyResolver2 CreateResolver()
        {
            var resolver = new CompilationDependencyResolver2(TestOutputHelper.CreateTestLogFactory());
            return resolver;
        }
    }
}