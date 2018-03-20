#! "netcoreapp2.0"
#r "nuget: Castle.Core,*"

Process.Start("dotnet", "--version").WaitForExit();
Console.WriteLine("Hello World");