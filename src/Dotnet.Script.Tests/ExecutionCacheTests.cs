using Dotnet.Script.Shared.Tests;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
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
            using var scriptFolder = new DisposableFolder();
            var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

            var cachePaths = new string[]
            {
                null,
                Path.Combine(scriptFolder.Path, "AlternateCachePath"),
                Path.Combine("Relative", "AlternateCachePath"),
            };

            foreach (var cachePath in cachePaths)
            {
                WriteScript(pathToScript, "WriteLine(42);");
                var (output, hash) = Execute(pathToScript, cachePath);
                Assert.Contains("42", output);
                Assert.NotNull(hash);

                WriteScript(pathToScript, "WriteLine(42);");
                var secondResult = Execute(pathToScript, cachePath);
                Assert.Contains("42", secondResult.output);
                Assert.NotNull(secondResult.hash);

                Assert.Equal(hash, secondResult.hash);
            }
        }

        [Fact]
        public void ShouldUpdateHashWhenSourceChanges()
        {
            using var scriptFolder = new DisposableFolder();
            var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

            var cachePaths = new string[]
            {
                null,
                Path.Combine(scriptFolder.Path, "AlternateCachePath"),
                Path.Combine("Relative", "AlternateCachePath"),
            };

            foreach (var cachePath in cachePaths)
            {
                WriteScript(pathToScript, "WriteLine(42);");
                var (output, hash) = Execute(pathToScript, cachePath);
                Assert.Contains("42", output);
                Assert.NotNull(hash);

                WriteScript(pathToScript, "WriteLine(84);");
                var secondResult = Execute(pathToScript, cachePath);
                Assert.Contains("84", secondResult.output);
                Assert.NotNull(secondResult.hash);

                Assert.NotEqual(hash, secondResult.hash);
            }
        }

        [Fact]
        public void ShouldNotCreateHashWhenScriptIsNotCacheable()
        {
            using var scriptFolder = new DisposableFolder();
            var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

            WriteScript(pathToScript, "#r \"nuget:AutoMapper, *\"", "WriteLine(42);");

            var cachePaths = new string[]
            {
                null,
                Path.Combine(scriptFolder.Path, "AlternateCachePath"),
                Path.Combine("Relative", "AlternateCachePath"),
            };

            foreach (var cachePath in cachePaths)
            {
                var (output, hash) = Execute(pathToScript, cachePath);
                Assert.Contains("42", output);

                Assert.Null(hash);
            }
        }

        [Fact]
        public void ShouldCopyDllAndPdbToExecutionCacheFolder()
        {
            using var scriptFolder = new DisposableFolder();
            var pathToScript = Path.Combine(scriptFolder.Path, "main.csx");

            var cachePaths = new string[]
            {
                null,
                Path.Combine(scriptFolder.Path, "AlternateCachePath"),
                Path.Combine("Relative", "AlternateCachePath"),
            };

            foreach (var cachePath in cachePaths)
            {
                WriteScript(pathToScript, "#r \"nuget:LightInject, 5.2.1\"", "WriteLine(42);");
                ExecuteScript(pathToScript, noCache: true, cachePath: cachePath);
                var pathToExecutionCache = GetPathToExecutionCache(pathToScript, cachePath);
                Assert.True(File.Exists(Path.Combine(pathToExecutionCache, "LightInject.dll")));
                Assert.True(File.Exists(Path.Combine(pathToExecutionCache, "LightInject.pdb")));
            }
        }

        [Fact]
        public void ShouldCacheScriptsFromSameFolderIndividually()
        {
            static (string Output, bool Cached, string SavedWhere) Execute(string pathToScript, string cachePath)
            {
                var result = ExecuteScript(pathToScript, debug: true, cachePath: cachePath);

                var isCached = result.Output.Contains("Using cached compilation");
                var matchWhere = isCached
                    ? Regex.Match(result.Output, "Using cached compilation: (.*)")
                    : Regex.Match(result.Output, "Project file saved to (.*)");

                return (
                    Output: result.Output,
                    Cached: isCached,
                    SavedWhere: matchWhere.Success ? matchWhere.Groups[1].Value : string.Empty
                );
            }

            using var scriptFolder = new DisposableFolder();
            var pathToScriptA = Path.Combine(scriptFolder.Path, "script.csx");
            var pathToScriptB = Path.Combine(scriptFolder.Path, "script");


            var idScriptA = Guid.NewGuid().ToString();
            File.AppendAllText(pathToScriptA, $@"WriteLine(""{idScriptA}"");");

            var idScriptB = Guid.NewGuid().ToString();
            File.AppendAllText(pathToScriptB, $@"WriteLine(""{idScriptB}"");");

            var cachePaths = new string[]
            {
                null,
                Path.Combine(scriptFolder.Path, "AlternateCachePath"),
                Path.Combine("Relative", "AlternateCachePath"),
            };

            foreach (var cachePath in cachePaths)
            {
                var firstResultOfScriptA = Execute(pathToScriptA, cachePath);
                Assert.Contains(idScriptA, firstResultOfScriptA.Output);
                Assert.False(firstResultOfScriptA.Cached);

                var firstResultOfScriptB = Execute(pathToScriptB, cachePath);
                Assert.Contains(idScriptB, firstResultOfScriptB.Output);
                Assert.False(firstResultOfScriptB.Cached);


                var secondResultOfScriptA = Execute(pathToScriptA, cachePath);
                Assert.Contains(idScriptA, secondResultOfScriptA.Output);
                Assert.True(secondResultOfScriptA.Cached);

                var secondResultOfScriptB = Execute(pathToScriptB, cachePath);
                Assert.Contains(idScriptB, secondResultOfScriptB.Output);
                Assert.True(secondResultOfScriptB.Cached);

                var idScriptB2 = Guid.NewGuid().ToString();
                File.AppendAllText(pathToScriptB, $@"WriteLine(""{idScriptB2}"");");

                var thirdResultOfScriptA = Execute(pathToScriptA, cachePath);
                Assert.Contains(idScriptA, thirdResultOfScriptA.Output);
                Assert.True(thirdResultOfScriptA.Cached);

                var thirdResultOfScriptB = Execute(pathToScriptB, cachePath);
                Assert.Contains(idScriptB, thirdResultOfScriptB.Output);
                Assert.Contains(idScriptB2, thirdResultOfScriptB.Output);
                Assert.False(thirdResultOfScriptB.Cached);

                if (cachePath != null)
                {
                    var cachePathWithoutRoot = Path.IsPathRooted(cachePath)
                        ? cachePath.Substring(Path.GetPathRoot(cachePath).Length)
                        : cachePath;

                    Assert.Contains(cachePathWithoutRoot, firstResultOfScriptA.SavedWhere);
                    Assert.Contains(cachePathWithoutRoot, firstResultOfScriptB.SavedWhere);
                    Assert.Contains(cachePathWithoutRoot, secondResultOfScriptA.SavedWhere);
                    Assert.Contains(cachePathWithoutRoot, secondResultOfScriptB.SavedWhere);
                    Assert.Contains(cachePathWithoutRoot, thirdResultOfScriptA.SavedWhere);
                    Assert.Contains(cachePathWithoutRoot, thirdResultOfScriptB.SavedWhere);
                }
            }
        }

        [Fact]
        public void ShouldUseCachePathWhenProvided()
        {
            using var scriptFolder = new DisposableFolder();
            var pathToScript = Path.Combine(scriptFolder.Path, "script.csx");

            var executionPathA = DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(pathToScript, cachePath: null);
            Assert.True(Path.IsPathFullyQualified(executionPathA));
            Assert.Contains("script.csx", executionPathA);

            var fullCachePath = Path.Combine(scriptFolder.Path, "AlternateCachePath");
            var fullCachePathNoRoot = fullCachePath.Substring(Path.GetPathRoot(fullCachePath).Length);
            Assert.True(Path.IsPathFullyQualified(fullCachePath));

            var executionPathB = DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(pathToScript, cachePath: fullCachePath);
            Assert.True(Path.IsPathFullyQualified(executionPathB));
            Assert.Contains("script.csx", executionPathB);
            Assert.Contains(fullCachePathNoRoot, executionPathB);

            var relativeCachePath = Path.Combine("Relative", "CachePath");
            Assert.False(Path.IsPathRooted(relativeCachePath));

            var executionPathC = DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(pathToScript, cachePath: relativeCachePath);
            Assert.True(Path.IsPathFullyQualified(executionPathC));
            Assert.Contains("script.csx", executionPathC);
            Assert.Contains(relativeCachePath, executionPathC);
        }

        private (string output, string hash) Execute(string pathToScript, string cachePath)
        {
            var result = ExecuteScript(pathToScript, cachePath: cachePath);
            testOutputHelper.WriteLine(result.Output);
            Assert.Equal(0, result.ExitCode);
            string pathToExecutionCache = GetPathToExecutionCache(pathToScript, cachePath);
            var pathToCacheFile = Path.Combine(pathToExecutionCache, "script.sha256");
            string cachedhash = null;
            if (File.Exists(pathToCacheFile))
            {
                cachedhash = File.ReadAllText(pathToCacheFile);
            }

            return (result.Output, cachedhash);
        }

        private static string GetPathToExecutionCache(string pathToScript, string cachePath)
        {
            var pathToTempFolder = Dotnet.Script.DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(pathToScript, cachePath);
            var pathToExecutionCache = Path.Combine(pathToTempFolder, "execution-cache");
            return pathToExecutionCache;
        }

        private static void WriteScript(string path, params string[] lines)
        {
            File.WriteAllLines(path, lines);
        }

        private static ProcessResult ExecuteScript(string pathToScript, bool debug = false, bool noCache = false, string cachePath = null)
        {
            var args = pathToScript +
                (debug ? " --debug" : string.Empty) +
                (noCache ? " --nocache" : string.Empty) +
                (cachePath != null ? $" --cache-path {cachePath}" : string.Empty);

            return ScriptTestRunner.Default.Execute(args);
        }
    }
}