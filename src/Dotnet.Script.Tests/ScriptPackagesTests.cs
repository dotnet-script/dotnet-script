using System;
using System.IO;
using System.Linq;
using System.Text;
using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Runtime;
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
        public void ShouldThrowMeaningfulExceptionWhenScriptPackageIsMissing()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var code = new StringBuilder();
                code.AppendLine("#load \"nuget:ScriptPackageWithMainCsx, 1.0.0\"");
                code.AppendLine("SayHello();");
                var pathToScriptFile = Path.Combine(scriptFolder.Path, "main.csx");
                File.WriteAllText(pathToScriptFile, code.ToString());
                var pathToScriptPackages = Path.GetFullPath(ScriptPackagesFixture.GetPathToPackagesFolder());

                // Run once to ensure that it is cached.
                var result = ScriptTestRunner.Default.Execute($"{pathToScriptFile} -s {pathToScriptPackages}");
                Assert.StartsWith("Hello from netstandard2.0", result.output);

                // Remove the package from the global NuGet cache
                TestPathUtils.RemovePackageFromGlobalNugetCache("ScriptPackageWithMainCsx");

                //Change the source to force a recompile, now with the missing package.
                code.Append("return 0;");
                File.WriteAllText(pathToScriptFile, code.ToString());

                result = ScriptTestRunner.Default.Execute($"{pathToScriptFile} -s {pathToScriptPackages}");
                Assert.Contains("Try executing/publishing the script", result.output);
            }
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
            var resolver = CreateRuntimeDependencyResolver();
            var fixture = GetFullPathToTestFixture("ScriptPackage/WithMainCsx");
            var csxFiles = Directory.GetFiles(fixture, "*.csx");
            var dependencies = resolver.GetDependencies(csxFiles.First(), Array.Empty<string>());
            var scriptFiles = dependencies.Single(d => d.Name == "ScriptPackageWithMainCsx").Scripts;
            Assert.NotEmpty(scriptFiles);
        }

        private static string GetFullPathToTestFixture(string path)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "TestFixtures", path);
        }


        private RuntimeDependencyResolver CreateRuntimeDependencyResolver()
        {
            var resolver = new RuntimeDependencyResolver(TestOutputHelper.CreateTestLogFactory(), useRestoreCache: false, useNugetCache: true);
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
                Program.Main(new[] { fullPathToScriptFile, "--no-cache" });
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