#! "netcoreapp2.0"
#r "nuget: Castle.Core,*"

Process.Start(new ProcessStartInfo {
    Arguments = "--version",
    FileName = "dotnet",
    RedirectStandardError = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
}).WaitForExit();
Console.WriteLine("Hello World");