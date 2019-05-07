using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.Shared.Tests;
using Xunit;

namespace Dotnet.Script.Desktop.Tests
{
    public class CompilationDependencyTests
    {
        [Fact]
        public void ShouldGetCompilationDependenciesForNetCoreApp2_1()
        {
            var resolver = CreateResolver();
        }

        private CompilationDependencyResolver CreateResolver()
        {
            var resolver = new CompilationDependencyResolver(TestOutputHelper.CreateTestLogFactory());
            return resolver;
        }
    }
}