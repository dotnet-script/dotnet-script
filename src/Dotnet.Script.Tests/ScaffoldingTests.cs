using System.IO;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ScaffoldingTests
    {
        [Fact]
        public void ShouldInitializeScriptFolder()
        {
            var tempFolder = CreateTempFolder();            
            var result = Execute("init", tempFolder);
            Assert.Equal(0, result.exitCode);
            Assert.True(File.Exists(Path.Combine(tempFolder, "helloworld.csx")));
            Assert.True(File.Exists(Path.Combine(tempFolder, "omnisharp.json")));
            Assert.True(File.Exists(Path.Combine(tempFolder, ".vscode", "launch.json")));
            Directory.Delete(tempFolder,true);
        }

        [Fact]
        public void ShouldCreateNewScript()
        {
            var tempFolder = CreateTempFolder();
            var result = Execute("new script.csx", tempFolder);
            Assert.Equal(0, result.exitCode);
            Assert.True(File.Exists(Path.Combine(tempFolder, "script.csx")));
            Directory.Delete(tempFolder, true);
        }


        private static string CreateTempFolder()
        {
            var userTempFolder = Path.GetTempPath();
            var tempFile = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            var tempFolder = Path.Combine(userTempFolder, tempFile);
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        /// <summary>
        /// Use this if you need to debug.
        /// </summary>        
        private static int ExecuteInProcess(params string[] args)
        {            
            return Program.Main(args);
        }

        private static (string output, int exitCode) Execute(string args, string workingDirectory)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(args), workingDirectory);
            return result;
        }

        private static string[] GetDotnetScriptArguments(string args)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            return new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, "netcoreapp2.0", "dotnet-script.dll"), args };
        }
    }

}