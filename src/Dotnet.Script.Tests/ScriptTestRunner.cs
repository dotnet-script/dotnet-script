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
        
        public (string output, int exitCode) Execute(string arguments, string workingDirectory = null)
        {
            var result = ProcessHelper.RunAndCaptureOutput2("dotnet", GetDotnetScriptArguments2(arguments), workingDirectory);
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

        public static int ExecuteCodeInProcess(string code, params string[] arguments)
        {
            var allArguments = new List<string>();
            if (arguments != null)
            {
                allArguments.AddRange(arguments);
            }
            allArguments.Add("eval");
            allArguments.Add(code);
            return Program.Main(allArguments.ToArray());
        }
        public (string output, int exitCode) ExecuteCode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(new[] {"eval", $"\"{code}\"" }));
            return result;
        }

        public (string output, int exitCode) ExecuteCodeInReleaseMode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(new[] {"-c", "release", "eval", $"\"{code}\"" }));
            return result;
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

        private string GetDotnetScriptArguments2(string arguments)
        {
            string configuration;
#if DEBUG
            configuration = "Debug";
#else
            configuration = "Release";
#endif
            var allArgs = $"exec {Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Dotnet.Script", "bin", configuration, _scriptEnvironment.TargetFramework, "dotnet-script.dll")} {arguments}";

            return allArgs;
        }
    }
}
