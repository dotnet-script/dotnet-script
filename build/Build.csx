#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"
#load "DotNet.csx"
#load "Choco.csx"
#load "NuGet.csx"

using System.Runtime.InteropServices;

var rootArg = Args.FirstOrDefault() ?? "..";
var root = Path.GetFullPath(rootArg);

BuildScriptPackages(root);

DotNet.Build(Path.Combine(root, "src","Dotnet.Script"));

DotNet.Test($"{root}/src/Dotnet.Script.Tests");


// We only publish packages from Windows/AppVeyor
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    string packagesOutputFolder = Path.Combine(root, "build", "NuGet"); 
    DotNet.Test($"{root}/src/Dotnet.Script.DependencyModel.Tests");
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.Core"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel"), packagesOutputFolder);
    DotNet.Pack(Path.Combine(root, "src" , "Dotnet.Script.DependencyModel.NuGet"), packagesOutputFolder);
    DotNet.Publish($"{root}/src/Dotnet.Script");
    Choco.Pack($"{root}/src/Dotnet.Script","Chocolatey");
}


private void BuildScriptPackages(string root)
{
    var pathToScriptPackages = Path.Combine(root,"src/Dotnet.Script.Tests/ScriptPackages");
    var specFiles = Directory.GetFiles(pathToScriptPackages,"*.nuspec",SearchOption.AllDirectories);
    foreach(var specFile in specFiles)
    {
        NuGet.Pack(specFile, Path.Combine(pathToScriptPackages,"packages"));
    }
}