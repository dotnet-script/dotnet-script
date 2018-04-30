using System.IO;
using Xunit;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.Tests
{
    public class ScaffoldingTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScaffoldingTests()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void ShouldInitializeScriptFolder()
        {
            using (var scriptFolder = new DisposableFolder())
            {                
                var result = Execute("init", scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "main.csx")));
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "omnisharp.json")));
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, ".vscode", "launch.json")));                
            }
        }

        [Fact]
        public void ShouldCreateEnableScriptNugetReferencesSetting()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var result = Execute("init", scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                dynamic settings = JObject.Parse(File.ReadAllText(Path.Combine(scriptFolder.Path, "omnisharp.json")));
                Assert.True(settings.script.enableScriptNuGetReferences.Value);
            }
        }

        [Fact]
        public void ShouldCreateDefaultTargetFrameworkSetting()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var result = Execute("init", scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                dynamic settings = JObject.Parse(File.ReadAllText(Path.Combine(scriptFolder.Path, "omnisharp.json")));
                Assert.Equal(_scriptEnvironment.TargetFramework, settings.script.defaultTargetFramework.Value);                
            }
        }

        [Fact]
        public void ShouldCreateNewScript()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var result = Execute("new script.csx", scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "script.csx")));
            }
        }

        [Fact]
        public void ShouldInitFolderWithCustomFileNAme()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var result = Execute("init custom.csx", scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "custom.csx")));
            }
        }

        [Fact]
        public void ShouldNotCreateDefaultFileForFolderWithExistingScriptFiles()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                Execute("init custom.csx", scriptFolder.Path);
                Execute("init", scriptFolder.Path);
                Assert.False(File.Exists(Path.Combine(scriptFolder.Path, "main.csx")));
            }
        }

        /// <summary>
        /// Use this if you need to debug.
        /// </summary>        
        private static int ExecuteInProcess(params string[] args)
        {            
            return Program.Main(args);
        }

        private (string output, int exitCode) Execute(string args, string workingDirectory)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(args), workingDirectory);
            return result;
        }

        private string[] GetDotnetScriptArguments(string args)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            return new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, _scriptEnvironment.TargetFramework, "dotnet-script.dll"), args };
        }
    }

}