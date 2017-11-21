using System.IO;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptProjectProviderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ScriptProjectProviderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ShouldCopyLocalNuGetConfig()
        {
            var provider = CreateProvider();
            var pathToProjectFile = provider.CreateProject(TestPathUtils.GetFullPathToTestFixture("LocalNuGetConfig"), "netcoreapp2.0", true);
            var pathToProjectFileFolder = Path.GetDirectoryName(pathToProjectFile);
            Assert.True(File.Exists(Path.Combine(pathToProjectFileFolder,"NuGet.Config")));
        }

        private ScriptProjectProvider CreateProvider()
        {
            ScriptProjectProvider provider = new ScriptProjectProvider(type => ((level, message) =>
            {
                _testOutputHelper.WriteLine($"{level}:{message ?? ""}");
            }));

            return provider;
        }
    }
}