using Xunit;
using Dotnet.Script.Shared.Tests;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class InteractiveRunnerTests : InteractiveRunnerTestsBase
    {
        public InteractiveRunnerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
