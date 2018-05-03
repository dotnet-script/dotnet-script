using Xunit;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;

namespace Dotnet.Script.Desktop.Tests
{
    [Collection("IntegrationTests")]
    public class InteractiveRunnerTests : InteractiveRunnerTestsBase
    {
        public InteractiveRunnerTests()
        {
            ScriptEnvironment.Default.OverrideTargetFramework("net461");
        }
    }
}
