using Xunit;
using Dotnet.Script.Shared.Tests;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class InteractiveRunnerTests : InteractiveRunnerTestsBase
    {
        public InteractiveRunnerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task ShouldCompileAndExecuteWithWebSdk()
        {
            var commands = new[]
            {
                @"#r ""sdk:Microsoft.NET.Sdk.Web""",
                "using Microsoft.AspNetCore.Builderss;",
                "var a = WebApplication.Create();",
                @"a.GetType()",
                "#exit"
            };

            var ctx = GetRunner(commands);
            await ctx.Runner.RunLoop();

            var result = ctx.Console.Out.ToString();

            Assert.Contains("[Microsoft.AspNetCore.Builder.WebApplication]", result);
        }
    }
}
