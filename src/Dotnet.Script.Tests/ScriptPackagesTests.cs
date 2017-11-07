using System;
using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScriptPackagesTests
    {
        public ScriptPackagesTests()
        {
            string pathToScriptPackages = "";
            Environment.SetEnvironmentVariable("ScriptPackagesSource",pathToScriptPackages,EnvironmentVariableTarget.User);
        }

        [Fact]
        public static void ShouldHandleScriptPackageWithMainCsx()
        {
            //var result = ExecuteInProcess($"{Path.Combine("ScriptPackage/WithMainCsx", "WithMainCsx.csx")}");
            //Assert.Equal("Hello from netstandard2.0", result.output);
        }
    }
}