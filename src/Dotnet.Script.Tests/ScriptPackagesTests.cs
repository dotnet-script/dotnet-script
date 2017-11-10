using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    public class ScriptPackagesTests
    {
        public ScriptPackagesTests(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.Capture();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pathToScriptPackages = Path.Combine(baseDir, "..", "..", "..", "ScriptPackages", "packages");            
            Environment.SetEnvironmentVariable("ScriptPackagesSource", pathToScriptPackages, EnvironmentVariableTarget.User);
        }

        private async Task<int> Execute(string scriptFileName)
        {
            ScriptLogger scriptLogger = new ScriptLogger(new TestOutputTextWriter(), true);
            ScriptCompiler compiler = new ScriptCompiler(scriptLogger, new RuntimeDependencyResolver(type => ((level, message) => TestOutputHelper.Current.WriteLine(message))));
            ScriptRunner scriptRunner = new ScriptRunner(compiler, scriptLogger);
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var fullPathToScriptFile = Path.Combine(baseDir, "..", "..", "..", "TestFixtures", "ScriptPackage", scriptFileName);
            var scriptContext = CreateScriptContext(fullPathToScriptFile, "debug",true, Array.Empty<string>(), false);
            return await scriptRunner.Execute<int>(scriptContext);
        }


        private static ScriptContext CreateScriptContext(string file, string config, bool debugMode, IEnumerable<string> args, bool interactive)
        {           
            var absoluteFilePath = Path.IsPathRooted(file) ? file : Path.Combine(Directory.GetCurrentDirectory(), file);
            var directory = Path.GetDirectoryName(absoluteFilePath);

            using (var filestream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sourceText = SourceText.From(filestream);
                return new ScriptContext(sourceText, directory, config, args, absoluteFilePath, debugMode);                               
            }
        }

        [Fact]
        public async Task ShouldHandleScriptPackageWithMainCsx()
        {
            var result = await Execute("WithMainCsx/WithMainCsx.csx");
            Assert.Equal(0,result);
            //Assert.Equal("Hello from netstandard2.0", result.output);
        }

        private static int ExecuteInProcess(string fixture, params string[] arguments)
        {
            var pathToFixture = Path.Combine("..", "..", "..", "TestFixtures", fixture);
            var allArguments = new List<string>(new[] { pathToFixture });
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            return Program.Main(allArguments.ToArray());
        }
    }

    public class TestOutputTextWriter : TextWriter
    {
        public override Encoding Encoding { get; }

        public override void WriteLine(string value)
        {
            TestOutputHelper.Current.WriteLine(value);
        }
    }
    

}