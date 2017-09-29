using System;
using System.Diagnostics;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.Process
{
    public class CommandRunner
    {
        private readonly Action<bool, string> _logger;

        public CommandRunner(Action<bool, string> logger)
        {
            _logger = logger;
        }

        public int Execute(string commandPath, string arguments = null)
        {
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
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            return startInformation;
        }

        private void RunAndWait(System.Diagnostics.Process process)
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
        private System.Diagnostics.Process CreateProcess(ProcessStartInfo startInformation)
        {
            var process = new System.Diagnostics.Process {StartInfo = startInformation};
            process.OutputDataReceived += (s, e) => _logger.Verbose(e.Data);
            process.ErrorDataReceived += (s, e) => _logger.Verbose(e.Data);
            return process;
        }
    }
}