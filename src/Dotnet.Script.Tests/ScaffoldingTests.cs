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
        public void ShouldRegisterToRunCsxScriptDirectly()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                Assert.Equal(0, exitCode);

                var scriptPath = Path.Combine(scriptFolder.Path, "main.csx");
                if (ScriptEnvironment.Default.IsWindows)
                {
                    (output, exitCode) = ProcessHelper.RunAndCaptureOutput("reg.exe", @"query HKCU\Software\Classes\.csx");
                    Assert.Equal(0, exitCode);
                    (output, exitCode) = ProcessHelper.RunAndCaptureOutput("reg.exe", @"query HKCU\software\classes\dotnetscript");
                    Assert.Equal(0, exitCode);
                }
                else
                {
                    var text = File.ReadAllText(scriptPath);
                    Assert.True(text.StartsWith("#!/usr/bin/env dotnet-script"), "should have shebang");
                    Assert.True(text.IndexOf("\r\n") < 0, "should have not have windows cr/lf");
                }
            }
        }

        [Fact]
        public void ShouldRunCsxScriptDirectly()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                Directory.CreateDirectory(scriptFolder.Path);
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                Assert.True(exitCode == 0, output);

                var scriptPath = Path.Combine(scriptFolder.Path, "main.csx");

                if (ScriptEnvironment.Default.IsWindows)
                {
                    (output, exitCode) = ProcessHelper.RunAndCaptureOutput("cmd.exe", $"/c \"{scriptPath}\"", scriptFolder.Path);
                    Assert.True(exitCode == 0, output);
                    Assert.Equal("Hello world!", output.Trim());
                }
                else
                {
                    // this depends on dotnet-script being installed as a dotnet global tool because the shebang needs to 
                    // point to an executable in the environment.  If you have dotnet-script installed as a global tool this
                    // test will pass
                    var (_, testExitCode) = ProcessHelper.RunAndCaptureOutput("dotnet-script", $"-h", scriptFolder.Path);
                    if (testExitCode == 0)
                    {
                        (output, exitCode) = ProcessHelper.RunAndCaptureOutput(scriptPath, "");
                        Assert.True(exitCode == 0, output);
                        Assert.Equal("Hello world!", output.Trim());
                    }
                }
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
