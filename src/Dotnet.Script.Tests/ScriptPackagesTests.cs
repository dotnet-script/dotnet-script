using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class ScriptPackagesTests : IClassFixture<ScriptPackagesFixture>
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptPackagesTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldHandleScriptPackageWithMainCsx()
        {
            var result = Execute("WithMainCsx/WithMainCsx.csx");
            Assert.StartsWith("Hello from netstandard2.0", result);
        }

        [Fact]
        public void ShouldHandleScriptWithAnyTargetFramework()
        {
            var result = Execute("WithAnyTargetFramework/WithAnyTargetFramework.csx");
            Assert.StartsWith("Hello from any target framework", result);
        }

        [Fact]
        public void ShouldHandleScriptPackageWithNoEntryPointFile()
        {
            var result = Execute("WithNoEntryPointFile/WithNoEntryPointFile.csx");
            Assert.Contains("Hello from Foo.csx", result);
            Assert.Contains("Hello from Bar.csx", result);
        }

        [Fact]
        public void ShouldHandleScriptPackageWithScriptPackageDependency()
        {
            var result = Execute("WithScriptPackageDependency/WithScriptPackageDependency.csx");
            Assert.StartsWith("Hello from netstandard2.0", result);
        }

        [Fact]
        public void ShouldThrowExceptionWhenReferencingUnknownPackage()
        {
            var result = Execute("WithInvalidPackageReference/WithInvalidPackageReference.csx");
            Assert.StartsWith("Unable to restore packages from", result);
        }

        [Fact]
        public void ShouldHandleScriptPackageWithSubFolder()
        {
            var result = Execute("WithSubFolder/WithSubFolder.csx");
            Assert.StartsWith("Hello from Bar.csx", result);
        }

        [Fact]
        public void ShouldGetScriptFilesFromScriptPackage()
        {
            var resolver = CreateCompilationDependencyResolver();
            var fixture = GetFullPathToTestFixture("ScriptPackage/WithMainCsx");
            var dependencies = resolver.GetDependencies(fixture, true, _scriptEnvironment.TargetFramework);
            var scriptFiles = dependencies.Single(d => d.Name == "ScriptPackageWithMainCsx").Scripts;
            Assert.NotEmpty(scriptFiles);
        }

        private static string GetFullPathToTestFixture(string path)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "TestFixtures", path);
        }


        private CompilationDependencyResolver CreateCompilationDependencyResolver()
        {
            var resolver = new CompilationDependencyResolver(TestOutputHelper.CreateTestLogFactory());
            return resolver;
        }

        private string Execute(string scriptFileName)
        {
            var output = new StringBuilder();
            var stringWriter = new StringWriter(output);
            var oldOut = Console.Out;
            var oldErrorOut = Console.Error;
            try
            {
                Console.SetOut(stringWriter);
                Console.SetError(stringWriter);
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var fullPathToScriptFile = Path.Combine(baseDir, "..", "..", "..", "TestFixtures", "ScriptPackage", scriptFileName);
                Program.Main(new[] { fullPathToScriptFile , "--nocache"});
                return output.ToString();

            }
            finally
            {
                Console.SetOut(oldOut);
                Console.SetError(oldErrorOut);
            }
        }
    }
}