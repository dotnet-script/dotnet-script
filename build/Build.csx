#! "netcoreapp2.0"
#load "nuget:Dotnet.Build, 0.3.1"
#load "nuget:github-changelog, 0.1.4"
#load "Choco.csx"
#load "BuildContext.csx"

using static ReleaseManagement;
using static ChangeLog;
using static FileUtils;
using System.Xml.Linq;



DotNet.Build(DotnetScriptProjectFolder);
DotNet.Test(TestProjectFolder);
DotNet.Publish(DotnetScriptProjectFolder, PublishArtifactsFolder, NetCoreApp20);

// We only publish packages from Windows/AppVeyor
if (BuildEnvironment.IsWindows)
{    
    
    using(var globalToolBuildFolder = new DisposableFolder())
    {
        Copy(SolutionFolder, globalToolBuildFolder.Path);
        PatchTargetFramework(globalToolBuildFolder.Path, NetCoreApp21);
        PatchPackAsTool(globalToolBuildFolder.Path);
        PatchPackageId(globalToolBuildFolder.Path, GlobalToolPackageId);
        DotNet.Pack(Path.Combine(globalToolBuildFolder.Path,"Dotnet.Script"),NuGetArtifactsFolder);
    }
    
    using(var nugetPackageBuildFolder = new DisposableFolder())
    {
        Copy(SolutionFolder, nugetPackageBuildFolder.Path);
        PatchTargetFramework(nugetPackageBuildFolder.Path, NetCoreApp20);        
        DotNet.Pack(Path.Combine(nugetPackageBuildFolder.Path,"Dotnet.Script"),NuGetArtifactsFolder);
    }
    
    DotNet.Pack(DotnetScriptCoreProjectFolder, NuGetArtifactsFolder);
    DotNet.Pack(DotnetScriptDependencyModelProjectFolder, NuGetArtifactsFolder);
    DotNet.Pack(DotnetScriptDependencyModelNuGetProjectFolder, NuGetArtifactsFolder);
            
    Choco.Pack(DotnetScriptProjectFolder, PublishArtifactsFolder, ChocolateyArtifactsFolder);
    Zip(PublishArchiveFolder, PathToGitHubReleaseAsset);
    
    if (BuildEnvironment.IsSecure)
    {
        await CreateReleaseNotes();

        if (Git.Default.IsTagCommit())
        {
            Git.Default.RequreCleanWorkingTree();
            await ReleaseManagerFor(Owner,ProjectName,BuildEnvironment.GitHubAccessToken)
            .CreateRelease(Git.Default.GetLatestTag(), PathToReleaseNotes, new [] { new ZipReleaseAsset(PathToGitHubReleaseAsset) });
            NuGet.TryPush(NuGetArtifactsFolder);
            Choco.TryPush(ChocolateyArtifactsFolder, BuildEnvironment.ChocolateyApiKey);
        }
    }
}

private async Task CreateReleaseNotes()
{
    Logger.Log("Creating release notes");        
    var generator = ChangeLogFrom(Owner, ProjectName, BuildEnvironment.GitHubAccessToken).SinceLatestTag();
    if (!Git.Default.IsTagCommit())
    {
        generator = generator.IncludeUnreleased();
    }
    await generator.Generate(PathToReleaseNotes);
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