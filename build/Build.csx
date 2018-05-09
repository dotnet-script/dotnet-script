#! "netcoreapp2.0"
#load "nuget:Dotnet.Build, 0.3.1"
#load "nuget:github-changelog, 0.1.5"
#load "Choco.csx"
#load "BuildContext.csx"

using static ReleaseManagement;
using static ChangeLog;
using static FileUtils;
using System.Xml.Linq;

DotNet.Build(dotnetScriptProjectFolder);
DotNet.Test(testProjectFolder);

// desktop CLR tests should only run on Windows
// we can run them on Mono in the future using the xunit runner instead of dotnet test
if (BuildEnvironment.IsWindows) 
{
    DotNet.Test(testDesktopProjectFolder);    
}

DotNet.Publish(dotnetScriptProjectFolder, publishArtifactsFolder, NetCoreApp20);

// We only publish packages from Windows/AppVeyor
if (BuildEnvironment.IsWindows)
{    
    
    using(var globalToolBuildFolder = new DisposableFolder())
    {
        Copy(solutionFolder, globalToolBuildFolder.Path);
        PatchTargetFramework(globalToolBuildFolder.Path, NetCoreApp21);
        PatchPackAsTool(globalToolBuildFolder.Path);
        PatchPackageId(globalToolBuildFolder.Path, GlobalToolPackageId);
        PatchContent(globalToolBuildFolder.Path);
        DotNet.Pack(Path.Combine(globalToolBuildFolder.Path,"Dotnet.Script"), nuGetArtifactsFolder);
    }
    
    using(var nugetPackageBuildFolder = new DisposableFolder())
    {
        Copy(solutionFolder, nugetPackageBuildFolder.Path);
        PatchTargetFramework(nugetPackageBuildFolder.Path, NetCoreApp20);        
        DotNet.Pack(Path.Combine(nugetPackageBuildFolder.Path,"Dotnet.Script"), nuGetArtifactsFolder);
    }
    
    DotNet.Pack(dotnetScriptCoreProjectFolder, nuGetArtifactsFolder);
    DotNet.Pack(dotnetScriptDependencyModelProjectFolder, nuGetArtifactsFolder);
    DotNet.Pack(dotnetScriptDependencyModelNuGetProjectFolder, nuGetArtifactsFolder);
            
    Choco.Pack(dotnetScriptProjectFolder, publishArtifactsFolder, chocolateyArtifactsFolder);
    Zip(publishArchiveFolder, pathToGitHubReleaseAsset);
    
    if (BuildEnvironment.IsSecure)
    {
        await CreateReleaseNotes();

        if (Git.Default.IsTagCommit())
        {
            Git.Default.RequreCleanWorkingTree();
            await ReleaseManagerFor(owner, projectName,BuildEnvironment.GitHubAccessToken)
            .CreateRelease(Git.Default.GetLatestTag(), pathToReleaseNotes, new [] { new ZipReleaseAsset(pathToGitHubReleaseAsset) });
            NuGet.TryPush(nuGetArtifactsFolder);
            Choco.TryPush(chocolateyArtifactsFolder, BuildEnvironment.ChocolateyApiKey);
        }
    }
}

private async Task CreateReleaseNotes()
{
    Logger.Log("Creating release notes");        
    var generator = ChangeLogFrom(owner, projectName, BuildEnvironment.GitHubAccessToken).SinceLatestTag();
    if (!Git.Default.IsTagCommit())
    {
        generator = generator.IncludeUnreleased();
    }
    await generator.Generate(pathToReleaseNotes);
}

private void PatchTargetFramework(string solutionFolder, string targetFramework)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder,"Dotnet.Script","Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var targetFrameworksElement = projectFile.Descendants("TargetFrameworks").Single();
    targetFrameworksElement.ReplaceWith(new XElement("TargetFramework",targetFramework));
    projectFile.Save(pathToDotnetScriptProject);
}

private void PatchPackAsTool(string solutionFolder)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder,"Dotnet.Script","Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var packAsToolElement = projectFile.Descendants("PackAsTool").Single();
    packAsToolElement.Value = "true";   
    projectFile.Save(pathToDotnetScriptProject); 
}

private void PatchPackageId(string solutionFolder, string packageId)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder,"Dotnet.Script","Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var packAsToolElement = projectFile.Descendants("PackageId").Single();
    packAsToolElement.Value = packageId;   
    projectFile.Save(pathToDotnetScriptProject); 
}

private void PatchContent(string solutionFolder)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder,"Dotnet.Script","Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var contentElements = projectFile.Descendants("Content").ToArray();
    foreach (var contentElement in contentElements)
    {
        contentElement.Remove();
    }
    projectFile.Save(pathToDotnetScriptProject);
}