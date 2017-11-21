using System.IO;
using System.Text;
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

        [Fact]
        public void ShouldLogProjectFileContent()
        {
            StringBuilder log = new StringBuilder();            
            var provider = new ScriptProjectProvider(type => ((level, message) => log.AppendLine(message)));

            provider.CreateProject(TestPathUtils.GetFullPathToTestFixture("Helloworld"), "netcoreapp2.0", true);
            var output = log.ToString();

            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">",output);
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