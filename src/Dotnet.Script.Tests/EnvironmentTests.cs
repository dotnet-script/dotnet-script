using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Text.RegularExpressions;


namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class EnvironmentTests
    {
        [Theory]
        [InlineData("-v")]
        [InlineData("--version")]
        public void ShouldPrintVersionNumber(string versionFlag)
        {
            var (output, exitCode) = ScriptTestRunner.Default.Execute(versionFlag);
            Assert.Equal(0, exitCode);
            // TODO test that version appears on first line of output!
            // semver regex from https://github.com/semver/semver/issues/232#issue-48635632
            Assert.Matches(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?$", output);
        }

        [Fact]
        public void ShouldPrintInfo()
        {
            var (output, exitCode) = ScriptTestRunner.Default.Execute("--info");
            Assert.Equal(0, exitCode);
            Assert.Contains("Version", output);
            Assert.Contains("Install location", output);
            Assert.Contains("Target framework", output);
            Assert.Contains(".NET Core version", output);
            Assert.Contains("Platform identifier", output);
            Assert.Contains("Runtime identifier", output);
        }

    }
}
