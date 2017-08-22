#load "Command.csx"
#load "FileUtils.csx"

public static class DotNet
{
    public static void Test(string pathToProjectFolder)
    {
        string pathToTestProject = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet","test " + pathToTestProject + " --configuration Release");   
    }
    
    public static void Pack(string pathToProjectFolder, string pathToPackageOutputFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet",$"pack {pathToProjectFile} --configuration Release --output {pathToPackageOutputFolder} ");   
    }

    public static void Build(string pathToProjectFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet","--version");
        Command.Execute("dotnet","restore " + pathToProjectFile);        
        Command.Execute("dotnet","build " + pathToProjectFile + " --configuration Release");   
    }

    private static string FindProjectFile(string pathToProjectFolder)
    {
        return FileUtils.FindFile(pathToProjectFolder, "*.csproj");
    }
}