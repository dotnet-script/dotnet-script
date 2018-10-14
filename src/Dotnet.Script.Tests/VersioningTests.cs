using System.Threading.Tasks;
using Dotnet.Script.Shared.Tests;
using Dotnet.Script.Core.Versioning;
using Xunit;
using Xunit.Abstractions;
using Moq;
using Dotnet.Script.Core;
using System.IO;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.Tests
{
    public class VersioningTests
    {
        public VersioningTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();                        
        }
       
        [Fact]
        public void ShouldGetCurrentVersion()
        {
            var versionProvider = new VersionProvider(TestOutputHelper.CreateTestLogFactory());
            
            var result = versionProvider.GetCurrentVersion();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetLatestVersion()
        {
            var versionProvider = new VersionProvider(TestOutputHelper.CreateTestLogFactory());
            
            var result = await versionProvider.GetLatestVersion();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldReportAboutNewVersin()
        {
            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(m => m.GetCurrentVersion()).Returns(new VersionInfo("X", true));
            versionProviderMock.Setup(m => m.GetLatestVersion()).ReturnsAsync(new VersionInfo("Y", true));

            StringWriter output = new StringWriter();
            StringWriter error = new StringWriter();                        
            ScriptConsole scriptConsole = new ScriptConsole(output,  StringReader.Null, error);
            var reporter = new EnvironmentReporter(versionProviderMock.Object,scriptConsole,ScriptEnvironment.Default);

            await reporter.ReportInfo();

            Assert.Contains("Version Y is now available", output.ToString());
        }
    }
}