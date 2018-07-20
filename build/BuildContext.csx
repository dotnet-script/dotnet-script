#load "nuget:Dotnet.Build, 0.3.1"
using static FileUtils;
using System.Xml.Linq;

const string NetCoreApp21 = "netcoreapp2.1";
const string GlobalToolPackageId = "dotnet-script";

var owner = "filipw";
var projectName = "dotnet-script";
var root = FileUtils.GetScriptFolder();
var solutionFolder = Path.Combine(root,"..","src");
var dotnetScriptProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script");
var dotnetScriptCoreProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script.Core");
var dotnetScriptDependencyModelProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script.DependencyModel");
var dotnetScriptDependencyModelNuGetProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script.DependencyModel.NuGet");
var testProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script.Tests");
var testDesktopProjectFolder = Path.Combine(root, "..", "src", "Dotnet.Script.Desktop.Tests");

var artifactsFolder = CreateDirectory(root, "Artifacts");
var gitHubArtifactsFolder = CreateDirectory(artifactsFolder, "GitHub");
var nuGetArtifactsFolder = CreateDirectory(artifactsFolder, "NuGet");
var chocolateyArtifactsFolder = CreateDirectory(artifactsFolder, "Chocolatey");
var publishArtifactsFolder = CreateDirectory(artifactsFolder, "Publish", projectName);
var publishArchiveFolder = Path.Combine(publishArtifactsFolder, "..");
var pathToReleaseNotes = Path.Combine(gitHubArtifactsFolder, "ReleaseNotes.md");

var version = ReadVersion();

var pathToGitHubReleaseAsset = Path.Combine(gitHubArtifactsFolder, $"{projectName}.{version}.zip");

string ReadVersion()
{
    var projectFile = XDocument.Load(Directory.GetFiles(dotnetScriptProjectFolder, "*.csproj").Single());
    var versionPrefix = projectFile.Descendants("VersionPrefix").SingleOrDefault()?.Value;
    var versionSuffix = projectFile.Descendants("VersionSuffix").SingleOrDefault()?.Value;

    if (versionSuffix != null)
    {
        return $"{versionPrefix}-{versionSuffix}";
    }
    else
    {
        return versionPrefix;
    }
}
