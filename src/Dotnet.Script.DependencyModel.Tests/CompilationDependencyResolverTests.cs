using System.Linq;
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
            var scriptProjectProvider = ScriptProjectProvider.Create((b, s) => _testOutputHelper.WriteLine(s));
            var project =
                scriptProjectProvider.CreateProject("../../../../Dotnet.Script.Tests/TestFixtures/InlineNuGetPackage",
                    "netcoreapp2.0");
            
            //var resolver = ScriptDependencyResolver.CreateCompilationResolver((verbose, message) =>
            //    _testOutputHelper.WriteLine(message ?? ""), enableScriptNuGetReferences:true);
            //var dependencies = resolver.GetDependencies("../../../../Dotnet.Script.Tests/TestFixtures/InlineNuGetPackage");
            //Assert.Contains("AutoMapper", dependencies.Select(d => d.Name));
        }
    }
}