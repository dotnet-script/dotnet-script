using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.Core.Versioning;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.Shared.Tests;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class VersioningTests
    {
        public VersioningTests(ITestOutputHelper testOutputHelper) => testOutputHelper.Capture();

        [Fact]
        public void ShouldGetCurrentVersion()
        {
            var versionProvider = new LoggedVersionProvider(TestOutputHelper.CreateTestLogFactory());
            
            var result = versionProvider.GetCurrentVersion();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetLatestVersion()
        {
            var versionProvider = new LoggedVersionProvider(TestOutputHelper.CreateTestLogFactory());
            
            var result = await versionProvider.GetLatestVersion();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldReportAboutNewVersion()
        {
            var output = await ReportWith("X","Y");
            Assert.Contains("Version Y is now available", output.ToString());
        }

        [Fact]
        public async Task ShouldNotReportLatestVersionWhenAlreayRunningLatest()
        {
            var output = await ReportWith("Y","Y");
            Assert.DoesNotContain("Version Y is now available", output.ToString());
        }

        private async Task<string> ReportWith(string currentVersion, string latestVersion)
        {
            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(m => m.GetCurrentVersion()).Returns(new VersionInfo(currentVersion, true));
            versionProviderMock.Setup(m => m.GetLatestVersion()).ReturnsAsync(new VersionInfo(latestVersion, true));

            StringWriter output = new StringWriter();
            StringWriter error = new StringWriter();                        
            ScriptConsole scriptConsole = new ScriptConsole(output,  StringReader.Null, error);
            var reporter = new EnvironmentReporter(versionProviderMock.Object,scriptConsole,ScriptEnvironment.Default);

            await reporter.ReportInfo();

            return output.ToString();
        }


        [Fact]
        public async Task ShouldLogErrorMessageWhenResolvingLatestVersionFails()
        {
            StringWriter log = new StringWriter();
            LogFactory logFactory = (type) => (level,message,exception) => 
            {
                log.WriteLine(message);
            };

            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(m => m.GetCurrentVersion()).Returns(new VersionInfo("X", true));
            versionProviderMock.Setup(m => m.GetLatestVersion()).ThrowsAsync(new Exception());

            var versionProvider = new LoggedVersionProvider(versionProviderMock.Object, logFactory);

            var reporter = new EnvironmentReporter(versionProvider, ScriptConsole.Default, ScriptEnvironment.Default);

            await reporter.ReportInfo();

            Assert.Contains("Failed to retrieve information about the latest version", log.ToString());
        } 
    }
}