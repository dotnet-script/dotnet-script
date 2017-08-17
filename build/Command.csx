#r "nuget:System.Diagnostics.Process, 4.3.0"
#load "Logger.csx"
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

public static class Command
{              
    public static void Execute(string commandPath, string arguments)
    {
        var startInformation = CreateProcessStartInfo(commandPath, arguments);
        var process = CreateProcess(startInformation);                        
        RunAndWait(process);                
               
        if (process.ExitCode != 0)
        {                                  
            throw new InvalidOperationException("Command failed");
        }                   
    }

    private static ProcessStartInfo CreateProcessStartInfo(string commandPath, string arguments)
    {
        var startInformation = new ProcessStartInfo($"\"{commandPath}\"");
        startInformation.CreateNoWindow = true;
        startInformation.Arguments =  arguments;
        startInformation.RedirectStandardOutput = true;
        startInformation.RedirectStandardError = true;
        startInformation.UseShellExecute = false;        
        return startInformation;
    }

    private static void RunAndWait(Process process)
    {        
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();         
        process.WaitForExit();                
    }
    private static Process CreateProcess(ProcessStartInfo startInformation)
    {
        var process = new Process();
        process.StartInfo = startInformation;  
        process.OutputDataReceived += (s,e) => Logger.Log(e.Data);
        process.ErrorDataReceived += (s,e) => Logger.Log(e.Data);
        return process;
    }
}