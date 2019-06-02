using System;
using System.Diagnostics;
using System.IO;

namespace Dotnet.Script.Shared.Tests
{
    public static class ProcessHelper
    {
        public static (string output, int exitcode) RunAndCaptureOutput(string fileName, string arguments, string workingDirectory = null)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            try
            {
                process.Start();
            }
            catch
            {
                Console.WriteLine($"Failed to launch '{fileName}' with args, '{arguments}'");
                return (null, -1);
            }

            var output = process.StandardOutput.ReadToEnd();
            output += process.StandardError.ReadToEnd();

            process.WaitForExit();

            return (output.Trim(), process.ExitCode);
        }
    }
}
