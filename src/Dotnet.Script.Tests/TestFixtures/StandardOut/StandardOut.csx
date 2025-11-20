
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

Command.Capture("dotnet", "--info");

public static class Command
{
    public static CommandResult Capture(string commandPath, string arguments, string workingDirectory = null)
    {
        return CaptureAsync(commandPath, arguments, workingDirectory).Result;
    }

    public static async Task<CommandResult> CaptureAsync(string commandPath, string arguments, string workingDirectory = null)
    {
        Error.WriteLine($"Executing command (CaptureAsync) {commandPath} {arguments} in working directory {workingDirectory}");

        var process = CreateProcess(commandPath, arguments, workingDirectory);

        var startProcessTask = StartProcessAsync(process, echo: false);
        var readStandardOutputTask = process.StandardOutput.ReadToEndAsync();
        var readStandardErrorTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(startProcessTask, readStandardOutputTask, readStandardErrorTask).ConfigureAwait(false);
        return new CommandResult(startProcessTask.Result, readStandardOutputTask.Result, readStandardErrorTask.Result);
    }

    public static async Task ExecuteAsync(string commandPath, string arguments, string workingDirectory = null, int success = 0)
    {
        Error.WriteLine($"Executing command {commandPath} {arguments} in working directory {workingDirectory}");
        var process = CreateProcess(commandPath, arguments, workingDirectory);
        RedirectToConsole(process);
        var exitCode = await StartProcessAsync(process, echo: true);
        if (exitCode != success)
        {
            throw new InvalidOperationException($"The command {commandPath} {arguments} failed.");
        }
    }

    public static void Execute(string commandPath, string arguments, string workingDirectory = null, int success = 0)
    {
        ExecuteAsync(commandPath, arguments, workingDirectory, success).Wait();
    }

    private static void RedirectToConsole(Process process)
    {
        process.OutputDataReceived += (o, a) => WriteStandardOut(a);
        process.ErrorDataReceived += (o, a) => WriteStandardError(a);
        void WriteStandardOut(DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Out.WriteLine(args.Data);
            }
        }

        void WriteStandardError(DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Error.WriteLine(args.Data);
            }
        }
    }

    private static Process CreateProcess(string commandPath, string arguments, string workingDirectory)
    {
        var startInformation = new ProcessStartInfo($"{commandPath}");
        startInformation.CreateNoWindow = true;
        startInformation.Arguments = arguments;
        startInformation.RedirectStandardOutput = true;
        startInformation.RedirectStandardError = true;
        startInformation.UseShellExecute = false;
        startInformation.WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        var process = new Process();
        process.StartInfo = startInformation;
        return process;
    }

    private static Task<int> StartProcessAsync(Process process, bool echo)
    {
        var tcs = new TaskCompletionSource<int>();
        process.Exited += (o, s) => tcs.SetResult(process.ExitCode);
        process.EnableRaisingEvents = true;
        process.Start();
        if (echo)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        return tcs.Task;
    }
}

public class CommandResult
{
    public CommandResult(int exitCode, string standardOut, string standardError)
    {
        ExitCode = exitCode;
        StandardOut = standardOut;
        StandardError = standardError;
    }
    public string StandardOut { get; }
    public string StandardError { get; }
    public int ExitCode { get; }

    public CommandResult Dump()
    {
        Out.Write(StandardOut);
        Error.Write(StandardError);
        return this;
    }

    public CommandResult EnsureSuccessfulExitCode(int success = 0)
    {
        if (ExitCode != success)
        {
            throw new InvalidOperationException(StandardError);
        }
        return this;
    }
}