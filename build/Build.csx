#load "nuget:Dotnet.Build, 0.23.0"
#load "nuget:dotnet-steps, 0.0.1"
#load "nuget:github-changelog, 0.1.5"
#load "BuildContext.csx"

using System.Xml.Linq;
using static ChangeLog;
using static FileUtils;
using static ReleaseManagement;

[StepDescription("Runs all tests.")]
Step test = () => RunTests();

[StepDescription("Creates all NuGet packages and the release zip for GitHub releases")]
Step pack = () =>
{
    CreateGitHubReleaseAsset();
    CreateNuGetPackages();
    CreateGlobalToolPackage();
};

[DefaultStep]
AsyncStep release = async () =>
{
    test();
    pack();
    await PublishRelease();
};

await StepRunner.Execute(Args);



private void CreateGitHubReleaseAsset()
{
    DotNet.Publish(dotnetScriptProjectFolder, publishArtifactsFolder, "net8.0");
    Zip(publishArchiveFolder, pathToGitHubReleaseAsset);
}

private void CreateGlobalToolPackage()
{
    using var globalToolBuildFolder = new DisposableFolder();
    Copy(solutionFolder, globalToolBuildFolder.Path);
    PatchPackAsTool(globalToolBuildFolder.Path);
    PatchPackageId(globalToolBuildFolder.Path, GlobalToolPackageId);
    PatchContent(globalToolBuildFolder.Path);
    Command.Execute("dotnet", $"pack --configuration release --output {nuGetArtifactsFolder}", Path.Combine(globalToolBuildFolder.Path, "Dotnet.Script"));
}

private void CreateNuGetPackages()
{
    Command.Execute("dotnet", $"pack --configuration release --output {nuGetArtifactsFolder}", dotnetScriptProjectFolder);
    Command.Execute("dotnet", $"pack --configuration release --output {nuGetArtifactsFolder}", dotnetScriptCoreProjectFolder);
    Command.Execute("dotnet", $"pack --configuration release --output {nuGetArtifactsFolder}", dotnetScriptDependencyModelProjectFolder);
    Command.Execute("dotnet", $"pack --configuration release --output {nuGetArtifactsFolder}", dotnetScriptDependencyModelNuGetProjectFolder);
}


private void RunTests()
{
    Command.Execute("dotnet", "test -c Release -f net8.0", testProjectFolder);
    Command.Execute("dotnet", "test -c Release -f net9.0", testProjectFolder);
    if (BuildEnvironment.IsWindows)
    {
        DotNet.Test(testDesktopProjectFolder);
    }
}

private async Task PublishRelease()
{
    if (!BuildEnvironment.IsWindows)
    {
        Logger.Log("Pushing a release can only be done from Windows because of Chocolatey");
        return;
    }

    if (!BuildEnvironment.IsSecure)
    {
        Logger.Log("Pushing a new release can only be done in a secure build environment");
        return;
    }

    await CreateReleaseNotes();

    if (Git.Default.IsTagCommit())
    {
        Git.Default.RequireCleanWorkingTree();
        await ReleaseManagerFor(owner, projectName, BuildEnvironment.GitHubAccessToken)
        .CreateRelease(Git.Default.GetLatestTag(), pathToReleaseNotes, new[] { new ZipReleaseAsset(pathToGitHubReleaseAsset) });

        DotNet.TryPush(nuGetArtifactsFolder);
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

private void PatchPackAsTool(string solutionFolder)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder, "Dotnet.Script", "Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var packAsToolElement = projectFile.Descendants("PackAsTool").Single();
    packAsToolElement.Value = "true";
    projectFile.Save(pathToDotnetScriptProject);
}

private void PatchPackageId(string solutionFolder, string packageId)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder, "Dotnet.Script", "Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var packAsToolElement = projectFile.Descendants("PackageId").Single();
    packAsToolElement.Value = packageId;
    projectFile.Save(pathToDotnetScriptProject);
}

private void PatchContent(string solutionFolder)
{
    var pathToDotnetScriptProject = Path.Combine(solutionFolder, "Dotnet.Script", "Dotnet.Script.csproj");
    var projectFile = XDocument.Load(pathToDotnetScriptProject);
    var contentElements = projectFile.Descendants("Content").ToArray();
    foreach (var contentElement in contentElements)
    {
        contentElement.Remove();
    }
    projectFile.Save(pathToDotnetScriptProject);
}