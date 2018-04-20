#load "nuget:Dotnet.Build, 0.3.1"
using static FileUtils;
using System.Xml.Linq;

const string NetCoreApp20 = "netcoreapp2.0";

const string NetCoreApp21 = "netcoreapp2.1";

const string GlobalToolPackageId = "dotnet-script";

string Version;

string GitHubArtifactsFolder;

string GitHubReleaseAsset;

string GitHubReleaseNoteAsset;

string NuGetArtifactsFolder;

string ChocolateyArtifactsFolder;

string PublishArtifactsFolder;

string PublishArchiveFolder;

string SolutionFolder;

string DotnetScriptProjectFolder;

string DotnetScriptCoreProjectFolder;

string DotnetScriptDependencyModelProjectFolder;

string DotnetScriptDependencyModelNuGetProjectFolder;

string Root;

string TestProjectFolder;

string PathToReleaseNotes;

string PathToGitHubReleaseAsset;

string Owner;

string ProjectName;

Owner = "filipw";
ProjectName = "dotnet-script";
Root = FileUtils.GetScriptFolder();
SolutionFolder = Path.Combine(Root,"..","src");
DotnetScriptProjectFolder = Path.Combine(Root, "..", "src", "Dotnet.Script");
DotnetScriptCoreProjectFolder = Path.Combine(Root, "..", "src", "Dotnet.Script.Core");
DotnetScriptDependencyModelProjectFolder = Path.Combine(Root, "..", "src", "Dotnet.Script.DependencyModel");
DotnetScriptDependencyModelNuGetProjectFolder = Path.Combine(Root, "..", "src", "Dotnet.Script.DependencyModel.NuGet");
TestProjectFolder = Path.Combine(Root, "..", "src", "Dotnet.Script.Tests");

var artifactsFolder = CreateDirectory(Root, "Artifacts");
GitHubArtifactsFolder = CreateDirectory(artifactsFolder, "GitHub");
NuGetArtifactsFolder = CreateDirectory(artifactsFolder, "NuGet");
ChocolateyArtifactsFolder = CreateDirectory(artifactsFolder, "Chocolatey");
PublishArtifactsFolder = CreateDirectory(artifactsFolder, "Publish", ProjectName);
PublishArchiveFolder = Path.Combine(PublishArtifactsFolder, "..");

PathToReleaseNotes = Path.Combine(GitHubArtifactsFolder, "ReleaseNotes.md");

Version = ReadVersion();

PathToGitHubReleaseAsset = Path.Combine(GitHubArtifactsFolder, $"{ProjectName}.{Version}.zip");

string ReadVersion()
{
    var projectFile = XDocument.Load(Directory.GetFiles(DotnetScriptProjectFolder, "*.csproj").Single());
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
