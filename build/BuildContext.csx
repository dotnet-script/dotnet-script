#load "nuget:Dotnet.Build, 0.2.7"
using static FileUtils;
using System.Xml.Linq;

public static class BuildContext
{
    static BuildContext()
    {
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
    }

    private static string Version { get; }

    public static string GitHubArtifactsFolder { get; }

    public static string GitHubReleaseAsset { get; }

    public static string GitHubReleaseNoteAsset { get; }

    public static string NuGetArtifactsFolder { get; }

    public static string ChocolateyArtifactsFolder { get; }

    public static string PublishArtifactsFolder { get; }

    public static string PublishArchiveFolder { get; }

    public static string DotnetScriptProjectFolder { get; }

    public static string DotnetScriptCoreProjectFolder { get; }

    public static string DotnetScriptDependencyModelProjectFolder { get; }

    public static string DotnetScriptDependencyModelNuGetProjectFolder { get; }

    public static string Root { get; }


    public static string TestProjectFolder { get; }

    public static string PathToReleaseNotes { get; }

    public static string PathToGitHubReleaseAsset { get; }
    public static string Owner { get; }
    public static string ProjectName { get; }
}