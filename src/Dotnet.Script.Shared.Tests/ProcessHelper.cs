using System;
using System.Diagnostics;
using System.IO;

namespace Dotnet.Script.Shared.Tests
{
    public static class ProcessHelper
    {
        public static ProcessResult RunAndCaptureOutput(string fileName, string arguments, string workingDirectory = null)
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
                return new ProcessResult(null, -1, null, null);
            }

            var standardOut = process.StandardOutput.ReadToEnd().Trim();
            var standardError = process.StandardError.ReadToEnd().Trim();

            var output = standardOut + standardError;

            process.WaitForExit();

            return new ProcessResult(output, process.ExitCode, standardOut, standardError);
        }
    }
}
