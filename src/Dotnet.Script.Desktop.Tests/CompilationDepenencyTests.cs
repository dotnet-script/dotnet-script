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

        [Fact]
        public void ShouldGetCompilationDependenciesForNetCoreApp2_1()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("HelloWorld");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, "netcoreapp2.1");
            Assert.True(dependencies.Count() > 0);
        }

        private CompilationDependencyResolver CreateResolver()
        {
            var resolver = new CompilationDependencyResolver(TestOutputHelper.CreateTestLogFactory());
            return resolver;
        }
    }
}