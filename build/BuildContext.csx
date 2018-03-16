#load "nuget:Dotnet.Build, 0.2.7"
using static FileUtils;
using System.Xml.Linq;

private static string Version;

public static string GitHubArtifactsFolder;

public static string GitHubReleaseAsset;

public static string GitHubReleaseNoteAsset;

public static string NuGetArtifactsFolder;

public static string ChocolateyArtifactsFolder;

public static string PublishArtifactsFolder;

public static string PublishArchiveFolder;

public static string DotnetScriptProjectFolder;

public static string DotnetScriptCoreProjectFolder;

public static string DotnetScriptDependencyModelProjectFolder;

public static string DotnetScriptDependencyModelNuGetProjectFolder;

public static string Root;

public static string TestProjectFolder;

public static string PathToReleaseNotes;

public static string PathToGitHubReleaseAsset;

public static string Owner;

public static string ProjectName;

Owner = "filipw";
ProjectName = "dotnet-script";
Root = FileUtils.GetScriptFolder();

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
