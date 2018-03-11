using Dotnet.Script.DependencyModel.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
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
            var dependencies =  resolver.GetDependencies(TestPathUtils.GetFullPathToTestFixture("InlineNugetPackage"), true, "netcoreapp2.0");
            Assert.Contains(dependencies, d => d.Name == "AutoMapper");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingNativeLibrary()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies(TestPathUtils.GetFullPathToTestFixture("NativeLibrary"), true, "netcoreapp2.0");
            Assert.Contains(dependencies, d => d.Name == "Microsoft.Data.Sqlite");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForIssue129()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies(TestPathUtils.GetFullPathToTestFixture("Issue129"), true, "netcoreapp2.0");
            Assert.Contains(dependencies, d => d.Name == "Auth0.ManagementApi");
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