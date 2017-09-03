#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"
#load "DotNet.csx"
#load "Choco.csx"

using System.Runtime.InteropServices;

DotNet.Build(@"../src/Dotnet.Script");
DotNet.Build(@"../src/Dotnet.Script.Tests");
DotNet.Test(@"../src/Dotnet.Script.Tests");
DotNet.Publish(@"../src/Dotnet.Script");

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Choco.Pack(@"../src/Dotnet.Script","Chocolatey");
}
