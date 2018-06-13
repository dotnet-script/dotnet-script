using Dotnet.Script.DependencyModel.Environment;
using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScriptPublisherTests
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptPublisherTests()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        [Fact]
        public void SimplePublishTest()
        {
            using (var scriptFolder = new DisposableFolder())
            {
                var code = @"WriteLine(""hello world"");";
                var mainPath = Path.Combine(scriptFolder.Path, "main.csx");
                File.WriteAllText(mainPath, code);
                var args = new string[] { "publish", mainPath };
                var result = Execute(string.Join(" ", args), scriptFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(scriptFolder.Path, "publish", "script.exe")));
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
                var result = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(publishRootFolder.Path, "script.exe")));
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
                var result = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(workspaceFolder.Path, "publish", "script.exe")));
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
                var result = Execute(string.Join(" ", args), workspaceFolder.Path);
                Assert.Equal(0, result.exitCode);
                Assert.True(File.Exists(Path.Combine(workspaceFolder.Path, "publish", "script.exe")));
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
