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

        static ScriptTestRunner()
        {
            // Redirect log messages to the test output window when running in process (DEBUG)
            Program.CreateLogFactory = (verbosity, debugMode) => TestOutputHelper.CreateTestLogFactory();
        }

        private ScriptTestRunner()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public (string output, int exitCode) Execute(string arguments, string workingDirectory = null)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(arguments), workingDirectory);
            return result;
        }

        public int ExecuteInProcess(string arguments = null)
        {
            return Program.Main(arguments?.Split(" ") ?? Array.Empty<string>());
        }

        public (string output, int exitCode) ExecuteFixture(string fixture, string arguments = null, string workingDirectory = null)
        {
            var pathToFixture = TestPathUtils.GetPathToTestFixture(fixture);
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"{pathToFixture} {arguments}"), workingDirectory);
            return result;
        }

        public (string output, int exitcode) ExecuteWithScriptPackage(string fixture, string arguments = null, string workingDirectory = null)
        {
            var pathToScriptPackageFixtures = TestPathUtils.GetPathToTestFixtureFolder("ScriptPackage");
            var pathToFixture = Path.Combine(pathToScriptPackageFixtures, fixture, $"{fixture}.csx");
            return ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"{pathToFixture} {arguments}"), workingDirectory);
        }

        public int ExecuteFixtureInProcess(string fixture, string arguments = null)
        {
            var pathToFixture = TestPathUtils.GetPathToTestFixture(fixture);
            return Program.Main(new[] { pathToFixture }.Concat(arguments?.Split(" ") ?? Array.Empty<string>()).ToArray());
        }

        public static int ExecuteCodeInProcess(string code, string arguments)
        {
            var allArguments = new List<string>();
            if (arguments != null)
            {
                allArguments.AddRange(arguments?.Split(" "));
            }
            allArguments.Add("eval");
            allArguments.Add(code);
            return Program.Main(allArguments.ToArray());
        }

        public (string output, int exitCode) ExecuteCode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"eval \"{code}\""));
            return result;
        }

        public (string output, int exitCode) ExecuteCodeInReleaseMode(string code)
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments($"-c release eval \"{code}\""));
            return result;
        }

        private string GetDotnetScriptArguments(string arguments)
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
