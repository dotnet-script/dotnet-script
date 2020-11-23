using Dotnet.Script.Shared.Tests;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ExecutionCacheTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public ExecutionCacheTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
            this.testOutputHelper = testOutputHelper;
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
                Assert.NotNull(firstResult.hash);

                WriteScript(pathToScript, "WriteLine(42);");
                var secondResult = Execute(pathToScript);
                Assert.Contains("42", secondResult.output);
                Assert.NotNull(secondResult.hash);

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
                Assert.NotNull(firstResult.hash);

                WriteScript(pathToScript, "WriteLine(84);");
                var secondResult = Execute(pathToScript);
                Assert.Contains("84", secondResult.output);
                Assert.NotNull(secondResult.hash);

                Assert.NotEqual(firstResult.hash, secondResult.hash);
            }
        }

        [Fact]
        public void ShouldNotCreateHashWhenScriptIsNotCacheable()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

                WriteScript(pathToScript, "#r \"nuget:AutoMapper, *\"", "WriteLine(42);");

                var result = Execute(pathToScript);
                Assert.Contains("42", result.output);

                Assert.Null(result.hash);
            }
        }

        [Fact]
        public void ShouldCopyDllAndPdbToExecutionCacheFolder()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

                WriteScript(pathToScript, "#r \"nuget:LightInject, 5.2.1\"", "WriteLine(42);");
                ScriptTestRunner.Default.Execute($"{pathToScript} --nocache");
                var pathToExecutionCache = GetPathToExecutionCache(pathToScript);
                Assert.True(File.Exists(Path.Combine(pathToExecutionCache, "LightInject.dll")));
                Assert.True(File.Exists(Path.Combine(pathToExecutionCache, "LightInject.pdb")));
            }
        }

        [Fact]
        public void ShouldCacheScriptsFromSameFolderIndividually()
        {
            (string Output, bool Cached) Execute(string pathToScript)
            {
                var result = ScriptTestRunner.Default.Execute($"{pathToScript} --debug");
                return (Output: result.output, Cached: result.output.Contains("Using cached compilation"));
            }

            using (var scriptFolder = new DisposableFolder())
            {
                var pathToScriptA = Path.Combine(scriptFolder.Path, "script.csx");
                var pathToScriptB = Path.Combine(scriptFolder.Path, "script");


                var idScriptA = Guid.NewGuid().ToString();
                File.AppendAllText(pathToScriptA, $@"WriteLine(""{idScriptA}"");");

                var idScriptB = Guid.NewGuid().ToString();
                File.AppendAllText(pathToScriptB, $@"WriteLine(""{idScriptB}"");");


                var firstResultOfScriptA = Execute(pathToScriptA);
                Assert.Contains(idScriptA, firstResultOfScriptA.Output);
                Assert.False(firstResultOfScriptA.Cached);

                var firstResultOfScriptB = Execute(pathToScriptB);
                Assert.Contains(idScriptB, firstResultOfScriptB.Output);
                Assert.False(firstResultOfScriptB.Cached);


                var secondResultOfScriptA = Execute(pathToScriptA);
                Assert.Contains(idScriptA, secondResultOfScriptA.Output);
                Assert.True(secondResultOfScriptA.Cached);

                var secondResultOfScriptB = Execute(pathToScriptB);
                Assert.Contains(idScriptB, secondResultOfScriptB.Output);
                Assert.True(secondResultOfScriptB.Cached);


                var idScriptB2 = Guid.NewGuid().ToString();
                File.AppendAllText(pathToScriptB, $@"WriteLine(""{idScriptB2}"");");


                var thirdResultOfScriptA = Execute(pathToScriptA);
                Assert.Contains(idScriptA, thirdResultOfScriptA.Output);
                Assert.True(thirdResultOfScriptA.Cached);

                var thirdResultOfScriptB = Execute(pathToScriptB);
                Assert.Contains(idScriptB, thirdResultOfScriptB.Output);
                Assert.Contains(idScriptB2, thirdResultOfScriptB.Output);
                Assert.False(thirdResultOfScriptB.Cached);
            }
        }

        private (string output, string hash) Execute(string pathToScript)
        {
            var result = ScriptTestRunner.Default.Execute(pathToScript);
            testOutputHelper.WriteLine(result.output);
            Assert.Equal(0, result.exitCode);
            string pathToExecutionCache = GetPathToExecutionCache(pathToScript);
            var pathToCacheFile = Path.Combine(pathToExecutionCache, "script.sha256");
            string cachedhash = null;
            if (File.Exists(pathToCacheFile))
            {
                cachedhash = File.ReadAllText(pathToCacheFile);
            }

            return (result.output, cachedhash);
        }

        private static string GetPathToExecutionCache(string pathToScript)
        {
            var pathToTempFolder = Dotnet.Script.DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(pathToScript);
            var pathToExecutionCache = Path.Combine(pathToTempFolder, "execution-cache");
            return pathToExecutionCache;
        }

        private static void WriteScript(string path, params string[] lines)
        {
            File.WriteAllLines(path, lines);
        }
    }
}