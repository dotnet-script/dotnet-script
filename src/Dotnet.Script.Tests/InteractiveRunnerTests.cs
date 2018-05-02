using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Runtime;
using Dotnet.Script.DependencyModel.Logging;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Text;
using System;
using Microsoft.CodeAnalysis.Text;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.Shared.Tests;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class InteractiveRunnerTests : InteractiveRunnerTestsBase
    {
    }
}
