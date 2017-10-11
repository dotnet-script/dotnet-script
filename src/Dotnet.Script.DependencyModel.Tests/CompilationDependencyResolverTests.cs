using System.Linq;
using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.DependencyModel.Tests
{
    public class CompilationDependencyResolverTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CompilationDependencyResolverTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingInlineNuGetPackageReference()
        {            
            var resolver = CreateResolver();
            var dependencies =  resolver.GetDependencies("../../../../Dotnet.Script.Tests/TestFixtures/InlineNuGetPackage", true, "netcoreapp2.0");
            Assert.True(dependencies.Any(d => d.Contains("AutoMapper")));
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingNativeLibrary()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies("../../../../Dotnet.Script.Tests/TestFixtures/NativeLibrary", true, "netcoreapp2.0");
            Assert.True(dependencies.Any(d => d.Contains("Microsoft.Data.Sqlite")));
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForIssue129()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies("../../../../Dotnet.Script.Tests/TestFixtures/Issue129", true, "netcoreapp2.0");
            Assert.True(dependencies.Any(d => d.Contains("Auth0.ManagementApi")));
        }

        private CompilationDependencyResolver CreateResolver()
        {           
            var resolver = new CompilationDependencyResolver(type => ((level, message) =>
            {
                _testOutputHelper.WriteLine($"{level}:{message ?? ""}");
            }) );
            return resolver;            
        }
    }
}