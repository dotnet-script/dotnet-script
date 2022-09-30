using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using System;
using System.IO;
using System.Linq;
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
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("InlineNugetPackage");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "AutoMapper");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingInlineNuGetPackageReferenceButSkipExcludedCsx()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("InlineNugetPackageWithFileFiltering");
            var csxFiles = Directory.GetFiles(targetDirectory, "InlineNugetPackage.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            Assert.DoesNotContain(dependencies, d => d.Name == "AutoMapper");
            Assert.Contains(dependencies, d => d.Name == "Newtonsoft.Json");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForPackageContainingNativeLibrary()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("NativeLibrary");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "Microsoft.Data.Sqlite.Core");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForNuGetPackageWithRefFolder()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("InlineNugetPackageWithRefFolder");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            var sqlClientDependency = dependencies.Single(d => d.Name.Equals("System.Data.SqlClient", StringComparison.InvariantCultureIgnoreCase));
            Assert.Contains(sqlClientDependency.AssemblyPaths, d => d.Replace("\\", "/").Contains("system.data.sqlclient/4.6.1/ref/"));
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForIssue129()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("Issue129");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies, d => d.Name == "Auth0.ManagementApi");
        }

        [Fact]
        public void ShouldGetCompilationDependenciesForWebSdk()
        {
            var resolver = CreateResolver();
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder("WebApi");
            var csxFiles = Directory.GetFiles(targetDirectory, "*.csx");
            var dependencies = resolver.GetDependencies(targetDirectory, csxFiles, true, _scriptEnvironment.TargetFramework);
            Assert.Contains(dependencies.SelectMany(d => d.AssemblyPaths), p => p.Contains("Microsoft.AspNetCore.Components"));
        }

        private CompilationDependencyResolver CreateResolver()
        {
            var resolver = new CompilationDependencyResolver(TestOutputHelper.CreateTestLogFactory());
            return resolver;
        }
    }
}