using Xunit;

namespace Dotnet.Script.Core.Tests
{
    public class ScriptExecutionTests
    {
        [Fact]
        public void ShouldExecuteHelloWorld()
        {
            Execute(@"..\..\..\TestFixtures\HelloWorld\HelloWorld.csx");
        }

        [Fact]
        public void ShouldExecuteScriptWithInlineNugetPackage()
        {
            Execute(@"..\..\..\TestFixtures\InlineNugetPackage\InlineNugetPackage.csx");
        }

        private static void Execute(string fixture)
        {
            var exitCode = Program.Main(new[] { fixture });
            Assert.Equal(0, exitCode);
        }
    }
}
