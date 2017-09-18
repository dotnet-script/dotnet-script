using System;
using System.Diagnostics;
using System.Text;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.Process
{
    public class Command
    {
        public static string Execute(string commandPath, string arguments, Action<bool, string> logAction)
        {
            var startInformation = CreateProcessStartInfo(commandPath, arguments);
            var output = new StringBuilder();
            var process = CreateProcess(startInformation, logAction, output);
            RunAndWait(process);            
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Command failed");
            }
            return output.ToString();
        }

        private static ProcessStartInfo CreateProcessStartInfo(string commandPath, string arguments)
        {
            var startInformation = new ProcessStartInfo($"{commandPath}");
            startInformation.CreateNoWindow = true;
            startInformation.Arguments = arguments;
            startInformation.RedirectStandardOutput = true;
            startInformation.RedirectStandardError = true;
            startInformation.UseShellExecute = false;
            return startInformation;
        }

        private static void RunAndWait(System.Diagnostics.Process process)
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
        private static System.Diagnostics.Process CreateProcess(ProcessStartInfo startInformation, Action<bool, string> logAction, StringBuilder output)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo = startInformation;
            process.OutputDataReceived += (s, e) =>
            {
                output.AppendLine(e.Data);
                logAction.Verbose(e.Data);
            };
            process.ErrorDataReceived += (s, e) => logAction.Verbose(e.Data);
            return process;
        }
    }
}