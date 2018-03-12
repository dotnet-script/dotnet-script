#load "nuget:Dotnet.Build, 0.2.5"

using System.Xml.Linq;

public static class Choco
{    
    /// <summary>
    /// Creates a Chocolatey package based on a "csproj" project file. 
    /// </summary>
    /// <param name="pathToProjectFolder">The path to the project folder.</param>
    /// <param name="outputFolder">The path to the output folder (*.nupkg)</param>
    public static void Pack(string pathToProjectFolder, string pathToBinaries, string outputFolder)
    {                        
        File.Copy(Path.Combine(pathToProjectFolder, "../../LICENSE"), Path.Combine("Chocolatey","tools","LICENSE.TXT"), true);
        string pathToProjectFile = Directory.GetFiles(pathToProjectFolder, "*.csproj").Single();
        CreateSpecificationFromProject(pathToProjectFile, pathToBinaries);        
        Command.Execute("choco.exe", $@"pack Chocolatey\chocolatey.nuspec  --outputdirectory {outputFolder}");      
    }

    public static void Push(string packagesFolder, string apiKey, string source = "https://push.chocolatey.org/")
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");        
        foreach(var packageFile in packageFiles)
        {            
            Command.Execute("choco.exe", $"push {packageFile} --source {source} --key {apiKey}");           
        }
    }

    private static void CreateSpecificationFromProject(string pathToProjectFile, string pathToBinaries)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var authors = projectFile.Descendants("Authors").SingleOrDefault()?.Value;
        var packageId = projectFile.Descendants("PackageId").SingleOrDefault()?.Value;
        var description = projectFile.Descendants("Description").SingleOrDefault()?.Value;
        var versionPrefix = projectFile.Descendants("VersionPrefix").SingleOrDefault()?.Value;
        var versionSuffix = projectFile.Descendants("VersionSuffix").SingleOrDefault()?.Value;
        
        string version;
        if (versionSuffix != null)
        {
            version = $"{versionPrefix}-{versionSuffix}";            
        }
        else
        {
            version = versionPrefix;
        }
        var tags = projectFile.Descendants("PackageTags").SingleOrDefault()?.Value;
        var iconUrl = projectFile.Descendants("PackageIconUrl").SingleOrDefault()?.Value;   
        var projectUrl = projectFile.Descendants("PackageProjectUrl").SingleOrDefault()?.Value;  
        var licenseUrl = projectFile.Descendants("PackageLicenseUrl").SingleOrDefault()?.Value;
        var repositoryUrl = projectFile.Descendants("RepositoryUrl").SingleOrDefault()?.Value;

        var packageElement = new XElement("package");
        var metadataElement = new XElement("metadata");      
        packageElement.Add(metadataElement);
        
        // Package id should be lower case
        // https://chocolatey.org/docs/create-packages#naming-your-package  
        metadataElement.Add(new XElement("id", packageId.ToLower()));    
        metadataElement.Add(new XElement("version", version));                
        metadataElement.Add(new XElement("authors", authors));        
        metadataElement.Add(new XElement("licenseUrl", licenseUrl));
        metadataElement.Add(new XElement("projectUrl", projectUrl));
        metadataElement.Add(new XElement("iconUrl", iconUrl));
        metadataElement.Add(new XElement("description", description));                
        metadataElement.Add(new XElement("tags", repositoryUrl));
        
        var filesElement = new XElement("files");
        packageElement.Add(filesElement);

        // Add the tools folder that contains "ChocolateyInstall.ps1"
        filesElement.Add(CreateFileElement(@"tools\*.*",$@"{packageId}\tools"));            
        var srcGlobPattern = $@"{pathToBinaries}\**\*";
        filesElement.Add(CreateFileElement(srcGlobPattern,packageId));
                                
        using (var fileStream = new FileStream("Chocolatey/chocolatey.nuspec",FileMode.Create))
        {
            new XDocument(packageElement).Save(fileStream);
        }
    }

    private static XElement CreateFileElement(string src, string target)
    {        
        var srcAttribute = new XAttribute("src", src);
        var targetAttribute = new XAttribute("target", target);
        return new XElement("file", srcAttribute, targetAttribute);
    }

}