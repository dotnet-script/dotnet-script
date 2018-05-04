using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.Tests
{
    public class ScriptTestRunner
    {
        public static readonly ScriptTestRunner Default = new ScriptTestRunner();

        private ScriptEnvironment _scriptEnvironment;

        private ScriptTestRunner()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
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
            var pathToFixture = Path.Combine("..", "..", "..", "TestFixtures", fixture);
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", GetDotnetScriptArguments(new string[] { pathToFixture}.Concat(arguments ?? Array.Empty<string>()).ToArray()));
            return result;
        }

        public int ExecuteFixtureInProcess(string fixture, params string[] arguments)
        {
            var pathToFixture = Path.Combine("..", "..", "..", "TestFixtures", fixture);
            return Program.Main(new[] { GetPathToFixture(fixture) }.Concat(arguments ?? Array.Empty<string>()).ToArray());
        }

        private static string GetPathToFixture(string fixture)
        {
            return Path.Combine("..", "..", "..", "TestFixtures", fixture);
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
