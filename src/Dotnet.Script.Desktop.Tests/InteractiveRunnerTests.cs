using Xunit;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using Xunit.Abstractions;

namespace Dotnet.Script.Desktop.Tests
{
    [Collection("IntegrationTests")]
    public class InteractiveRunnerTests : InteractiveRunnerTestsBase
    {
        public InteractiveRunnerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
