using System.Threading.Tasks;
using Dotnet.Script.Shared.Tests;
using Dotnet.Script.Core.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class VersioningTests
    {
        public VersioningTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }
       
        [Fact]
        public async Task ShouldGetLatestVersion()
        {
            var versionProvider = new LatestVersionProvider(TestOutputHelper.CreateTestLogFactory());
            
            var result = await versionProvider.GetVersion();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetInstalledVersion()
        {
            var versionProvider = new InstalledVersionProvider();
            
            var result = await versionProvider.GetVersion();

            Assert.NotNull(result);
        }
    }
}