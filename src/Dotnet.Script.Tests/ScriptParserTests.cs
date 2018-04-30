using System.Linq;
using System.Text;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptParserTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptParserTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldResolveSinglePackage()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode("#r \"nuget:Package, 1.2.3\"");

            Assert.Equal(1, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.Single().Id);
            Assert.Equal("1.2.3", result.PackageReferences.Single().Version);
        }

        [Fact]
        public void ShouldResolveSinglePackageFromLoadDirective()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode("#load \"nuget:Package, 1.2.3\"");

            Assert.Equal(1, result.PackageReferences.Count);
            Assert.Equal("Package", result.PackageReferences.Single().Id);
            Assert.Equal("1.2.3", result.PackageReferences.Single().Version);
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
            Assert.Equal("Package", result.PackageReferences.First().Id);
            Assert.Equal("1.2.3", result.PackageReferences.First().Version);
            Assert.Equal("AnotherPackage", result.PackageReferences.Last().Id);
            Assert.Equal("3.2.1", result.PackageReferences.Last().Version);
        }

        [Fact]
        public void ShouldParseTargetFramework()
        {
            var parser = CreateParser();

            var result = parser.ParseFromCode($"#! \"{_scriptEnvironment.TargetFramework}\"");

            Assert.Equal(_scriptEnvironment.TargetFramework, result.TargetFramework);            
        }

        private ScriptParser CreateParser()
        {
            return new ScriptParser(type => ((level, message) => _testOutputHelper.WriteLine(message)));
        }
    }
}