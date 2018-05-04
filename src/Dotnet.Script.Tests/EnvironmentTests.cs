using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Text.RegularExpressions;


namespace Dotnet.Script.Tests
{
    public class EnvironmentTests
    {
        [Fact]
        public void ShouldPrintVersionNumber()
        {
            var result = ScriptTestRunner.Default.Execute("--version");
            Assert.Equal(0, result.exitCode);
            Assert.Matches(@"\d*.\d*.\d*", result.output);

            result = ScriptTestRunner.Default.Execute("-v");
            Assert.Equal(0, result.exitCode);
            Assert.Matches(@"\d*.\d*.\d*", result.output);
        }

        [Fact]
        public void ShouldPrintInfo()
        {
            var result = ScriptTestRunner.Default.Execute("--info");
            Assert.Equal(0, result.exitCode);
            Assert.Contains("Version", result.output);
            Assert.Contains("Install location", result.output);
            Assert.Contains("Target framework", result.output);
            Assert.Contains("Platform identifier", result.output);
            Assert.Contains("Runtime identifier", result.output);
        }

    }
}
