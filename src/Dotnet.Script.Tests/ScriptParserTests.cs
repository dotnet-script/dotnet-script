using System.Linq;
using System.Text;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptParserTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptParserTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture(minimumLogLevel:0);
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldResolveSinglePackage()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode("#r \"nuget:Package, 1.2.3\"");

            Assert.Equal(1, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.Single().Id.Value);
            Assert.Equal("1.2.3", result.PackageReferences.Single().Version.Value);
        }

        [Fact]
        public void ShouldResolveSinglePackageFromLoadDirective()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode("#load \"nuget:Package, 1.2.3\"");

            Assert.Equal(1, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.Single().Id.Value);
            Assert.Equal("1.2.3", result.PackageReferences.Single().Version.Value);
        }

        [Theory]
        [InlineData("Package", "1.2.3-beta-1")]
        [InlineData("PACKAGE", "1.2.3-beta-1")]
        [InlineData("Package", "1.2.3-BETA-1")]
        public void ShouldResolveUniquePackages(string id, string version)
        {
            var parser = CreateParser();
            var code = new StringBuilder();
            code.AppendLine("#r \"nuget:Package, 1.2.3-beta-1\"");
            code.AppendLine($"#r \"nuget:{id}, {version}\"");

            var result = parser.ParseFromCode(code.ToString());

            Assert.Equal(1, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.Single().Id.Value  );
            Assert.Equal("1.2.3-beta-1", result.PackageReferences.Single().Version.Value);
        }

        [Fact]
        public void ShouldResolveMultiplePackages()
        {
            var parser = CreateParser();
            var code = new StringBuilder();
            code.AppendLine("#r \"nuget:Package, 1.2.3\"");
            code.AppendLine("#r \"nuget:AnotherPackage, 3.2.1\"");

            var result = parser.ParseFromCode(code.ToString());

            Assert.Equal(2, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.First().Id.Value);
            Assert.Equal("1.2.3", result.PackageReferences.First().Version.Value);
            Assert.Equal("AnotherPackage", result.PackageReferences.Last().Id.Value);
            Assert.Equal("3.2.1", result.PackageReferences.Last().Version.Value);
        }

        [Theory]
        [InlineData("\r #load\"nuget:Package, 1.2.3\"")]
        [InlineData("#load\n\"nuget:Package, 1.2.3\"")]
        [InlineData("#load \"nuget:\nPackage, 1.2.3\"")]
        [InlineData("#load \"nuget:Package\n, 1.2.3\"")]
        [InlineData("#load \"nuget:Package,\n1.2.3\"")]
        [InlineData("#load \"nuget:P a c k a g e, 1.2.3\"")]
        [InlineData("#load \"nuget:Pack/age, 1.2.3\"")]
        [InlineData("#load \"nuget:Package,\"")]
        [InlineData("\r #r\"nuget:Package, 1.2.3\"")]
        [InlineData("#r\n\"nuget:Package, 1.2.3\"")]
        [InlineData("#r \"nuget:\nPackage, 1.2.3\"")]
        [InlineData("#r \"nuget:Package\n, 1.2.3\"")]
        [InlineData("#r \"nuget:Package,\n1.2.3\"")]
        [InlineData("#r \"nuget:P a c k a g e, 1.2.3\"")]
        [InlineData("#r \"nuget:Pack/age, 1.2.3\"")]
        [InlineData("#r \"nuget:Package,\"")]
        public void ShouldNotMatchBadDirectives(string code)
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode(code);

            Assert.Equal(0, result.PackageReferences.Count);
        }

        [Fact]
        public void ShouldParseTargetFramework()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode($"#! \"{_scriptEnvironment.TargetFramework}\"");

            Assert.Equal(_scriptEnvironment.TargetFramework, result.TargetFramework);
        }

        [Theory]
        [InlineData("\n#! \"TARGET_FRAMEWORK\"")]
        [InlineData("\r#! \"TARGET_FRAMEWORK\"")]
        [InlineData("#!\n\"TARGET_FRAMEWORK\"")]
        [InlineData("#!\r\"TARGET_FRAMEWORK\"")]
        public void ShouldNotParseBadTargetFramework(string code)
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode(code.Replace("TARGET_FRAMEWORK", _scriptEnvironment.TargetFramework));

            Assert.Null(result.TargetFramework);
        }

        private ScriptParser CreateParser()
        {
            return new ScriptParser(TestOutputHelper.CreateTestLogFactory());
        }
    }
}