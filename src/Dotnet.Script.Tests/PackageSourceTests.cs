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

        [Fact]
        public void ShouldHandleSpecifyingPackageSourceWhenEvaluatingCode()
        {
            var fixture = "ScriptPackage/WithNoNuGetConfig";
            var pathToScriptPackages = TestPathUtils.GetPathToScriptPackages(fixture);            
            var code = @"#load \""nuget:ScriptPackageWithMainCsx,1.0.0\"" SayHello();";
            var result = ScriptTestRunner.Default.Execute("-s", pathToScriptPackages, "eval", $"\"{code}\"");            
            Assert.Equal(0, result.exitCode);
            Assert.Contains("Hello", result.output);
        }
    }
}
