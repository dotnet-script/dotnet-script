#load "FileUtils.csx"
#load "DotNet.csx"
#load "Command.csx"

using System.Xml.Linq;

public static class Choco
{    
    /// <summary>
    /// Creates a Chocolatey package based on a "csproj" project file. 
    /// </summary>
    /// <param name="pathToProjectFolder">The path to the project folder.</param>
    /// <param name="outputFolder">The path to the output folder (*.nupkg)</param>
    public static void Pack(string pathToProjectFolder, string outputFolder)
    {        
        DotNet.Publish(pathToProjectFolder);        
        Directory.CreateDirectory(outputFolder);        
        var pathToPublishFolder = FileUtils.FindDirectory(Path.Combine(pathToProjectFolder, @"bin\Release"),"publish");        
        File.Copy(Path.Combine(pathToProjectFolder, "../../LICENSE"), Path.Combine("Chocolatey","tools","LICENSE.TXT"), true);
        string pathToProjectFile = FileUtils.FindFile(pathToProjectFolder, "*.csproj");
        CreateSpecificationFromProject(pathToProjectFile, pathToPublishFolder);        
        Command.Execute("choco.exe", $@"pack Chocolatey\chocolatey.nuspec  --outputdirectory {outputFolder}");      
    }

    private static void CreateSpecificationFromProject(string pathToProjectFile, string pathToPublishFolder)
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
        filesElement.Add(CreateFileElement(@"tools\*.*",@"Dotnet.Script\tools"));    
        var srcFolder = Path.Combine("../",pathToPublishFolder).WithWindowsSlashes();
        var srcGlobPattern = $@"{srcFolder}\**\*";
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