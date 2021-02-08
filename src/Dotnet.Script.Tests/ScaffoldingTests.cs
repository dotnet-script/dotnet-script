using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScaffoldingTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScaffoldingTests(ITestOutputHelper testOutputHelper)
        {
            _scriptEnvironment = ScriptEnvironment.Default;
            testOutputHelper.Capture();
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
        public void ShouldInitializeScriptFolderContainingWhitespace()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var path = Path.Combine(scriptFolder.Path, "Folder with whitespace");
                Directory.CreateDirectory(path);

                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", path);

                Assert.Equal(0, exitCode);
                Assert.DoesNotContain("No such file or directory", output, StringComparison.OrdinalIgnoreCase);
                Assert.True(File.Exists(Path.Combine(path, "main.csx")));
                Assert.True(File.Exists(Path.Combine(path, "omnisharp.json")));
                Assert.True(File.Exists(Path.Combine(path, ".vscode", "launch.json")));
            }
        }

        [OnlyOnUnixFact]
        public void ShouldRegisterToRunCsxScriptDirectly()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                Assert.True(exitCode == 0, output);

                var scriptPath = Path.Combine(scriptFolder.Path, "main.csx");
                var text = File.ReadAllText(scriptPath);
                Assert.True(text.StartsWith("#!/usr/bin/env dotnet-script"), "should have shebang");
                Assert.True(text.IndexOf("\r\n") < 0, "should have not have windows cr/lf");
            }
        }

        [OnlyOnUnixFact]
        public void ShouldRunCsxScriptDirectly()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                Directory.CreateDirectory(scriptFolder.Path);
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);
                Assert.True(exitCode == 0, output);

                var scriptPath = Path.Combine(scriptFolder.Path, "main.csx");

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

        [Fact]
        public void ShouldCreateEnableScriptNugetReferencesSetting()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var (output, exitCode) = ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                Assert.Equal(0, exitCode);

                dynamic settings = JObject.Parse(File.ReadAllText(Path.Combine(scriptFolder.Path, "omnisharp.json")));

                Assert.True((bool)settings.script.enableScriptNuGetReferences.Value);
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

                Assert.Equal(_scriptEnvironment.TargetFramework, (string)settings.script.defaultTargetFramework.Value);
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

                config.SelectToken("configurations[0].args[1]").Replace("InvalidPath/dotnet-script.dll");

                File.WriteAllText(pathToLaunchConfiguration, config.ToString());

                ScriptTestRunner.Default.Execute("init", scriptFolder.Path);

                config = JObject.Parse(File.ReadAllText(pathToLaunchConfiguration));
                Assert.NotEqual("InvalidPath/dotnet-script.dll", config.SelectToken("configurations[0].args[1]").Value<string>());
            }
        }

        [Fact]
        public void ShouldCreateUnifiedLaunchFileWhenInstalledAsGlobalTool()
        {
            Scaffolder scaffolder = CreateTestScaffolder("somefolder/.dotnet/tools/dotnet-script");

            using (var scriptFolder = new DisposableFolder())
            {
                scaffolder.InitializerFolder("main.csx", scriptFolder.Path);
                var fileContent = File.ReadAllText(Path.Combine(scriptFolder.Path, ".vscode", "launch.json"));
                Assert.Contains("{env:HOME}/.dotnet/tools/dotnet-script", fileContent);
            }
        }

        [Fact]
        public void ShouldUpdateToUnifiedLaunchFileWhenInstalledAsGlobalTool()
        {
            Scaffolder scaffolder = CreateTestScaffolder("some-install-folder");
            Scaffolder globalToolScaffolder = CreateTestScaffolder("somefolder/.dotnet/tools/dotnet-script");
            using (var scriptFolder = new DisposableFolder())
            {
                scaffolder.InitializerFolder("main.csx", scriptFolder.Path);
                var fileContent = File.ReadAllText(Path.Combine(scriptFolder.Path, ".vscode", "launch.json"));
                Assert.Contains("some-install-folder", fileContent);
                globalToolScaffolder.InitializerFolder("main.csx", scriptFolder.Path);
                fileContent = File.ReadAllText(Path.Combine(scriptFolder.Path, ".vscode", "launch.json"));
                Assert.Contains("{env:HOME}/.dotnet/tools/dotnet-script", fileContent);
            }
        }

        private static Scaffolder CreateTestScaffolder(string installLocation)
        {
            var scriptEnvironment = (ScriptEnvironment)Activator.CreateInstance(typeof(ScriptEnvironment), nonPublic: true);
            var installLocationField = typeof(ScriptEnvironment).GetField("_installLocation", BindingFlags.NonPublic | BindingFlags.Instance);
            installLocationField.SetValue(scriptEnvironment, new Lazy<string>(() => installLocation));
            var scriptConsole = new ScriptConsole(StringWriter.Null, StringReader.Null, StreamWriter.Null);
            var scaffolder = new Scaffolder(TestOutputHelper.CreateTestLogFactory(), scriptConsole, scriptEnvironment);
            return scaffolder;
        }
    }
}
