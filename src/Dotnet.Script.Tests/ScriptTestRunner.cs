using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;

namespace Dotnet.Script.Tests
{
    public class ScriptTestRunner
    {
        public static readonly ScriptTestRunner Default = new ScriptTestRunner();

        private ScriptEnvironment _scriptEnvironment;

        private ScriptTestRunner()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
            Program.CreateLogFactory = (verbosity, debugMode) => TestOutputHelper.CreateTestLogFactory();
        }


        public (string output, int exitCode) Execute(params string[] arguments)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(arguments));
            return result;
        }

        public int ExecuteInProcess(params string[] arguments)
        {                        
            return Program.Main(arguments ?? Array.Empty<string>());
        }

        public (string output, int exitCode) ExecuteFixture(string fixture, params string[] arguments)
        {
            var pathToFixture = TestPathUtils.GetPathToTestFixture(fixture);
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(new string[] { pathToFixture}.Concat(arguments ?? Array.Empty<string>()).ToArray()));
            return result;
        }

        public int ExecuteFixtureInProcess(string fixture, params string[] arguments)
        {
            var pathToFixture = TestPathUtils.GetPathToTestFixture(fixture);            
            return Program.Main(new[] { pathToFixture }.Concat(arguments ?? Array.Empty<string>()).ToArray());
        }
        
        private string[] GetDotnetScriptArguments(params string[] arguments)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            var allArguments = new List<string>(new[] { "exec", Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, _scriptEnvironment.TargetFramework, "dotnet-script.dll")});
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            return allArguments.ToArray();
        }
    }
}
