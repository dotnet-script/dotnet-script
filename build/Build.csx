#! "netcoreapp2.0"
#r "nuget:NetStandard.Library,2.0.0"
#load "DotNet.csx"
#load "Choco.csx"
#load "NuGet.csx"
#load "FileUtils.csx"

using System.Runtime.InteropServices;


var currentFolder = Path.GetDirectoryName(GetScriptPath());
var root = Path.GetFullPath(Path.Combine(currentFolder, ".."));


DotNet.Build(Path.Combine(root, "src","Dotnet.Script"));

DotNet.Test($"{root}/src/Dotnet.Script.Tests");


// We only publish packages from Windows/AppVeyor
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    string packagesOutputFolder = Path.Combine(root, "build", "NuGet");     
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.Core"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel.NuGet"), packagesOutputFolder);
    DotNet.Publish($"{root}/src/Dotnet.Script");
    Choco.Pack($"{root}/src/Dotnet.Script","Chocolatey");
}


