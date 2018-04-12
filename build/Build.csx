#! "netcoreapp2.0"
#load "nuget:Dotnet.Build, 0.2.9"
#load "nuget:github-changelog, 0.1.4"
#load "Choco.csx"
#load "BuildContext.csx"

using static ReleaseManagement;
using static ChangeLog;
using static FileUtils;

DotNet.Build(DotnetScriptProjectFolder);
DotNet.Test(TestProjectFolder);
DotNet.Publish(DotnetScriptProjectFolder, PublishArtifactsFolder);

// We only publish packages from Windows/AppVeyor
if (BuildEnvironment.IsWindows)
{
    NuGet.PackAsTool(DotnetScriptProjectFolder,PublishArtifactsFolder,NuGetArtifactsFolder);
    DotNet.Pack(DotnetScriptProjectFolder, NuGetArtifactsFolder);
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
