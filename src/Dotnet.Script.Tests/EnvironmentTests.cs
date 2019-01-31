using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Text.RegularExpressions;


namespace Dotnet.Script.Tests
{
    public class EnvironmentTests
    {
        [Theory]
        [InlineData("-v")]
        [InlineData("--version")]
        public void ShouldPrintVersionNumber(string versionFlag)
        {
            var result = ScriptTestRunner.Default.Execute(versionFlag);

            if (result.exitCode != 0)
            {
                Console.WriteLine(result.output);
            }

            Assert.Equal(0, result.exitCode);
            // TODO test that version appears on first line of output!
            Assert.Matches(@"^[0-9]+(\.[0-9]+){2}$", result.output);
        }

        [Fact]
        public void ShouldPrintInfo()
        {
            var result = ScriptTestRunner.Default.Execute("--info");
            Assert.Equal(0, result.exitCode);
            Assert.Contains("Version", result.output);
            Assert.Contains("Install location", result.output);
            Assert.Contains("Target framework", result.output);
            Assert.Contains(".NET Core version", result.output);
            Assert.Contains("Platform identifier", result.output);
            Assert.Contains("Runtime identifier", result.output);
        }

    }
}
