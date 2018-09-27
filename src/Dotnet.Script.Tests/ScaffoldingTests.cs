using Dotnet.Script.DependencyModel.Environment;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;


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
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "main.csx")));
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "omnisharp.json")));
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, ".vscode", "launch.json")));
            }
        }

        [Fact]
        public void ShouldRunCsxScriptDirectly()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                var scriptPath = Path.Combine(scriptFolder.Path, "main.csx");

                if (ScriptEnvironment.Default.IsWindows)
                {
                    (output, exitCode) = ProcessHelper.RunAndCaptureOutput("cmd.exe", $"/c {scriptPath}", scriptFolder.Path);
                }
                else
                {
                    (output, exitCode) = ProcessHelper.RunAndCaptureOutput(scriptPath, string.Empty, scriptFolder.Path);
                }
                Assert.Equal("Hello world!", output.Trim());
            }
        }

        [Fact]
        public void ShouldCreateEnableScriptNugetReferencesSetting()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                Assert.Equal(0, exitCode);

                dynamic settings = JObject.Parse(File.ReadAllText(Path.Combine(scriptFolder.Path, "omnisharp.json")));

                Assert.True(settings.script.enableScriptNuGetReferences.Value);
            }
        }

        [Fact]
        public void ShouldCreateDefaultTargetFrameworkSetting()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var result = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

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
                var (output, exitCode) = ScriptTestRunner.Default.Execute("new script.csx", scriptFolder.Path);

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "script.csx")));
            }
        }

        [Fact]
        public void ShouldCreateNewScriptWithExtension()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("new anotherScript", scriptFolder.Path);

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "anotherScript.csx")));
            }
        }

        [Fact]
        public void ShouldInitFolderWithCustomFileName()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init custom.csx", scriptFolder.Path);

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "custom.csx")));
            }
        }

        [Fact]
        public void ShouldInitFolderWithCustomFileNameAndExtension()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init anotherCustom", scriptFolder.Path);

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "anotherCustom.csx")));
            }
        }

        [Fact]
        public void ShouldNotCreateDefaultFileForFolderWithExistingScriptFiles()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                ScriptTestRunner.Default.Execute("init custom.csx", scriptFolder.Path);
                ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                Assert.False(File.Exists(Path.Combine(scriptFolder.Path, "main.csx")));
            }
        }

        [Fact]
        public void ShouldUpdatePathToDotnetScript()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                var pathToLaunchConfiguration = Path.Combine(scriptFolder.Path, ".vscode/launch.json");
                var config = JObject.Parse(File.ReadAllText(pathToLaunchConfiguration));

                config.SelectToken("configurations[0].args[1]").Replace("InvalidPath/dotnet-script.dll,");

                File.WriteAllText(pathToLaunchConfiguration, config.ToString());

                ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                config = JObject.Parse(File.ReadAllText(pathToLaunchConfiguration));
                Assert.NotEqual("InvalidPath/dotnet-script.dll", config.SelectToken("configurations[0].args[1]").Value<string>());
            }
        }
    }

}
