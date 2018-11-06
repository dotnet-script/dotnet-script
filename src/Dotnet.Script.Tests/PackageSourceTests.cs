using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class PackageSourceTests : IClassFixture<ScriptPackagesFixture>
    {
        public PackageSourceTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldHandleSpecifyingPackageSource()
        {
            var fixture = "ScriptPackage/WithNoNuGetConfig";
            var pathToScriptPackages = TestPathUtils.GetPathToScriptPackages(fixture);
            var result = ScriptTestRunner.Default.ExecuteFixture(fixture, $"--nocache -s {pathToScriptPackages}");
            Assert.Contains("Hello", result.output);
            Assert.Equal(0, result.exitCode);
        }

        [Fact]
        public void ShouldHandleSpecifyingPackageSourceWhenEvaluatingCode()
        {
            var fixture = "ScriptPackage/WithNoNuGetConfig";
            var pathToScriptPackages = TestPathUtils.GetPathToScriptPackages(fixture);
            var code = @"#load \""nuget:ScriptPackageWithMainCsx,1.0.0\"" SayHello();";
            var result = ScriptTestRunner.Default.Execute($"--nocache -s {pathToScriptPackages} eval \"{code}\"");
            Assert.Contains("Hello", result.output);
            Assert.Equal(0, result.exitCode);
        }
    }
}
