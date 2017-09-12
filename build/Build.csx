#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"
#load "DotNet.csx"
#load "Choco.csx"

using System.Runtime.InteropServices;

var root = Args.FirstOrDefault() ?? "..";

DotNet.Build($"{root}/src/Dotnet.Script");
DotNet.Build($"{root}/src/Dotnet.Script.Tests");
DotNet.Test($"{root}/src/Dotnet.Script.Tests");
DotNet.Publish($"{root}/src/Dotnet.Script");

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Choco.Pack($"{root}/src/Dotnet.Script","Chocolatey");
}
