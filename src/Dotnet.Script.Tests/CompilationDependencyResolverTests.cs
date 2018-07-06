using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class CompilationDependencyResolverTests
    {        
        private readonly ScriptEnvironment _scriptEnvironment;

        public CompilationDependencyResolverTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingInlineNuGetPackageReference()
        {            
            var resolver = CreateResolver();
            var dependencies =  resolver.GetDependencies(TestPathUtils.GetPathToTestFixtureFolder("InlineNugetPackage"), true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "AutoMapper");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingNativeLibrary()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies(TestPathUtils.GetPathToTestFixtureFolder("NativeLibrary"), true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "Microsoft.Data.Sqlite");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForIssue129()
        {
            var resolver = CreateResolver();
            var dependencies = resolver.GetDependencies(TestPathUtils.GetPathToTestFixtureFolder("Issue129"), true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "Auth0.ManagementApi");
        }       
        private CompilationDependencyResolver CreateResolver()
        {
            var resolver = new CompilationDependencyResolver(TestOutputHelper.CreateTestLogFactory());
            return resolver;            
        }
    }
}