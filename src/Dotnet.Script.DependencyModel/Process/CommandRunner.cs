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
            return startInformation;
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