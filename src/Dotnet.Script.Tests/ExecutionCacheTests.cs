using System.IO;
using Dotnet.Script.Shared.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ExecutionCacheTests
    {
        public ExecutionCacheTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
        }

        [Fact]
        public void ShouldNotUpdateHashWhenSourceIsNotChanged()
        {
             using (var scriptFolder = new DisposableFolder())
            {
                var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

                WriteScript(pathToScript, "WriteLine(42);");
                var firstResult = Execute(pathToScript);
                Assert.Contains("42", firstResult.output);

                WriteScript(pathToScript, "WriteLine(42);");
                var secondResult = Execute(pathToScript);
                Assert.Contains("42", secondResult.output);

                Assert.Equal(firstResult.hash, secondResult.hash);
            }
        }


        [Fact]
        public void ShouldUpdateHashWhenSourceChanges()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

                WriteScript(pathToScript, "WriteLine(42);");
                var firstResult = Execute(pathToScript);
                Assert.Contains("42", firstResult.output);

                WriteScript(pathToScript, "WriteLine(84);");
                var secondResult = Execute(pathToScript);
                Assert.Contains("84", secondResult.output);

                Assert.NotEqual(firstResult.hash, secondResult.hash);
            }
        }

        [Fact]
        public void ShouldNotCreateHashWhenScriptIsNotCacheable()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

                WriteScript(pathToScript, "#r \"nuget:AutoMapper, 7.0\"" ,"WriteLine(42);");

                var result = Execute(pathToScript);
                Assert.Contains("42", result.output);

                Assert.Null(result.hash);
            }
        }


        private static (string output, string hash) Execute(string pathToScript)
        {
            var result = ScriptTestRunner.Default.Execute(pathToScript);
            Assert.Equal(0, result.exitCode);

            var pathToTempFolder = Path.GetDirectoryName(Dotnet.Script.DependencyModel.ProjectSystem.FileUtils.GetPathToTempFolder(pathToScript));
            var pathToExecutionCache = Path.Combine(pathToTempFolder, "execution-cache");
            var pathToCacheFile = Path.Combine(pathToExecutionCache, "script.sha256");
            string cachedhash = null;
            if (File.Exists(pathToCacheFile))
            {
                cachedhash = File.ReadAllText(pathToCacheFile);
            }
            return (result.output, cachedhash);
        }

        private static void WriteScript(string path, params string[] lines)
        {
            File.WriteAllLines(path, lines);
        }
    }
}