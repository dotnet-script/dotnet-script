#load "Command.csx"

public static class NuGet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";
    
    private static string ApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
           
    public static void Push(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");        
        foreach(var packageFile in packageFiles)
        {            
            try
            {
                Command.Execute("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}");
            }
            catch (Exception ex)
            {
                // If we come here it is probably because the package already exists.
                Logger.Log(ex.ToString());
            }            
        }
    }

    public static void Pack(string pathToMetadata, string outputDirectory)
    {
        Command.Execute("nuget",$"pack {pathToMetadata} -OutputDirectory {outputDirectory}");
    }
}