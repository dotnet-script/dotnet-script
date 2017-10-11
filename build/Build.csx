#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"
#load "DotNet.csx"
#load "Choco.csx"

using System.Runtime.InteropServices;

var rootArg = Args.FirstOrDefault() ?? "..";
var root = Path.GetFullPath(rootArg);

DotNet.Build(Path.Combine(root, "src","Dotnet.Script"));

DotNet.Test($"{root}/src/Dotnet.Script.Tests");
DotNet.Test($"{root}/src/Dotnet.Script.DependencyModel.Tests");

string packagesOutputFolder = Path.Combine(root, "build", "NuGet"); 
DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script"), packagesOutputFolder);
DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.Core"), packagesOutputFolder);
DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel"), packagesOutputFolder);
DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel.NuGet"), packagesOutputFolder);


DotNet.Publish($"{root}/src/Dotnet.Script");

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Choco.Pack($"{root}/src/Dotnet.Script","Chocolatey");
}
