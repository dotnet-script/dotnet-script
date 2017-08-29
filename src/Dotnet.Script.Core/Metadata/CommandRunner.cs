using System;
using System.Diagnostics;
using System.Text;

namespace Dotnet.Script.Core.Metadata
{
    /// <summary>
    /// A class that is capable of running a command.
    /// </summary>
    public class CommandRunner 
    {        
        private readonly StringBuilder _lastStandardErrorOutput = new StringBuilder();
        private readonly StringBuilder _lastProcessOutput = new StringBuilder();
        private readonly ScriptLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandRunner"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ScriptLogger"/> used for logging.</param>
        public CommandRunner(ScriptLogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Execute(string commandPath, string arguments)
        {
            _lastStandardErrorOutput.Clear();

            _logger.Verbose($"Executing {commandPath} {arguments}");
            var startInformation = CreateProcessStartInfo(commandPath, arguments);
            var process = CreateProcess(startInformation);
            RunAndWait(process);
            _logger.Verbose(_lastProcessOutput.ToString());
            if (process.ExitCode != 0)
            {
                _logger.Log(_lastStandardErrorOutput.ToString());
                throw new InvalidOperationException($"The command {commandPath} {arguments} failed to execute");
            }
            return _lastProcessOutput.ToString();
        }

        private static ProcessStartInfo CreateProcessStartInfo(string commandPath, string arguments)
        {
            var startInformation = new ProcessStartInfo(commandPath);
            startInformation.CreateNoWindow = true;
            startInformation.Arguments = arguments;
            startInformation.RedirectStandardOutput = true;
            startInformation.RedirectStandardError = true;
            startInformation.UseShellExecute = false;
            return startInformation;
        }

        private Process CreateProcess(ProcessStartInfo startInformation)
        {
            var process = new Process();
            process.StartInfo = startInformation;
            process.ErrorDataReceived += (s, a) =>
            {
                if (!string.IsNullOrWhiteSpace(a.Data))
                {
                    _lastStandardErrorOutput.AppendLine(a.Data);
                }

            };
            process.OutputDataReceived += (s, a) =>
            {
                _lastProcessOutput.AppendLine(a.Data);
            };
            return process;
        }

        private static void RunAndWait(Process process)
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
    }
}