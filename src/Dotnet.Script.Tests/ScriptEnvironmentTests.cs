using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScriptEnvironmentTests
    {
        [Fact]
        public void ShouldGetTargetFramework()
        {
            var codeBase = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.CodeBase;
        }
    }
}