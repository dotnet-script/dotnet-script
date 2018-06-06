using Xunit;

namespace Dotnet.Script.Tests
{
    public class PackageSourceTests : IClassFixture<ScriptPackagesFixture>
    {
        [Fact]
        public void ShouldHandleSpecifyingPackageSource()
        {
            var fixture = "ScriptPackage/WithNoNuGetConfig";
            var pathToScriptPackages = TestPathUtils.GetPathToScriptPackages(fixture);            
            var result = ScriptTestRunner.Default.ExecuteFixture(fixture, "-s", pathToScriptPackages);
            Assert.Equal(0, result.exitCode);
            Assert.Contains("Hello", result.output);
        }
    }
}
