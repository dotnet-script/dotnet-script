using System.Collections.Generic;
using System.Diagnostics;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.Process
{
    public class CommandRunner
    {
        private readonly Logger _logger;

        public CommandRunner(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<CommandRunner>();
        }

        public int Execute(string commandPath, string arguments = null)
        {
            _logger.Debug($"Executing '{commandPath} {arguments}'");
            var startInformation = CreateProcessStartInfo(commandPath, arguments);            
            var process = CreateProcess(startInformation);
            RunAndWait(process);
            return process.ExitCode;
        }

        private static ProcessStartInfo CreateProcessStartInfo(string commandPath, string arguments)
        {
            var startInformation = new ProcessStartInfo($"{commandPath}")
            {
                CreateNoWindow = true,
                Arguments = arguments ?? "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,                
                UseShellExecute = false
            };
            RemoveMsBuildEnvironmentVariables(startInformation.Environment);
            return startInformation;
        }
        private static void RemoveMsBuildEnvironmentVariables(IDictionary<string, string> environment)
        {
            // Remove various MSBuild environment variables set by OmniSharp to ensure that
            // the .NET CLI is not launched with the wrong values.
            environment.Remove("MSBUILD_EXE_PATH");
            environment.Remove("MSBuildExtensionsPath");
        }


        private static void RunAndWait(System.Diagnostics.Process process)
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
        private System.Diagnostics.Process CreateProcess(ProcessStartInfo startInformation)
        {
            var process = new System.Diagnostics.Process {StartInfo = startInformation};
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.Debug(e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.Debug(e.Data);
                }
            };
            return process;
        }
    }
}