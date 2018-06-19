using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScriptPublisherTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly CommandRunner _commandRunner;

        public ScriptPublisherTests()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
            _commandRunner = new CommandRunner(GetLogFactory());
        }

        [Fact]
        public void SimplePublishTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var args = new string[] { "publish", mainPath };
                var publishResult = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", "script");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            using (var publishRootFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var args = new string[] { "publish", mainPath, "-o", publishRootFolder.Path };
                var publishResult = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(publishRootFolder.Path, "script");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishFromCurrentDirectoryTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var args = new string[] { "publish", "main.csx" };
                var publishResult = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", "script");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishFromCurrentDirectoryToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var args = new string[] { "publish", "main.csx", "-o", "publish" };
                var publishResult = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var exePath = Path.Combine(workspaceFolder.Path, "publish", "script");
                var executableRunResult = _commandRunner.Execute(exePath);

                Assert.Equal(0, executableRunResult);
            }
        }

        [Fact]
        public void SimplePublishDllTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishArgs = new string[] { "publish", mainPath, "--dll" };
                var publishResult = Execute(string.Join(" ", publishArgs), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine("publish", "main.dll");
                var executeArgs = new string[] { "exec", dllPath };
                var dllRunResult = Execute(string.Join(" ", executeArgs), workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void SimplePublishDllFromCurrentDirectoryTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishArgs = new string[] { "publish", "main.csx", "--dll" };
                var publishResult = Execute(string.Join(" ", publishArgs), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(workspaceFolder.Path, "publish", "main.dll");
                var executeArgs = new string[] { "exec", dllPath };
                var dllRunResult = Execute(string.Join(" ", executeArgs), workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void SimplePublishDllToOtherFolderTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            using (var publishFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishArgs = new string[] { "publish", mainPath, "--dll", "-o", publishFolder.Path };
                var publishResult = Execute(string.Join(" ", publishArgs), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(publishFolder.Path, "main.dll");
                var executeArgs = new string[] { "exec", dllPath };
                var dllRunResult = Execute(string.Join(" ", executeArgs), publishFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
            }
        }

        [Fact]
        public void CustomDllNameTest()
        {
            using (var workspaceFolder = new DisposableFolder())
            {
                var outputName = "testName";
                var assemblyName = $"{outputName}.dll";
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(workspaceFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var publishArgs = new string[] { "publish", "main.csx", "--dll", "-n", outputName };
                var publishResult = Execute(string.Join(" ", publishArgs), workspaceFolder.Path);
                Assert.Equal(0, publishResult.exitCode);

                var dllPath = Path.Combine(workspaceFolder.Path, "publish", assemblyName);
                var executeArgs = new string[] { "exec", dllPath };
                var dllRunResult = Execute(string.Join(" ", executeArgs), workspaceFolder.Path);

                Assert.Equal(0, dllRunResult.exitCode);
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

        private LogFactory GetLogFactory()
        {
            var logger = new ScriptLogger(ScriptConsole.Default.Error, true);
            return type => ((level, message) =>
            {
                if (level == LogLevel.Debug)
                {
                    logger.Verbose(message);
                }
                if (level == LogLevel.Info)
                {
                    logger.Log(message);
                }
            });
        }
    }
}
