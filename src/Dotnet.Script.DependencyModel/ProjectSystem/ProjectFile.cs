using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Dotnet.Script.DependencyModel.Parsing;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public class ProjectFile
    {
        private readonly XDocument _document;

        public ProjectFile()
        {
            var template = ReadTemplate("csproj.template");
            _document = XDocument.Parse(template);
        }

        public void AddPackageReference(PackageReference packageReference)
        {
            var itemGroupElement = _document.Descendants("ItemGroup").Single();
            var packageReferenceElement = new XElement("PackageReference");
            packageReferenceElement.Add(new XAttribute("Include", packageReference.Id));
            packageReferenceElement.Add(new XAttribute("Version", packageReference.Version));
            itemGroupElement.Add(packageReferenceElement);
        }

        public void SetTargetFramework(string targetFramework)
        {
            var targetFrameworkElement = _document.Descendants("TargetFramework").Single();
            targetFrameworkElement.Value = targetFramework;
        }


        public void Save(string pathToProjectFile)
        {
            using (var fileStream = new FileStream(pathToProjectFile, FileMode.Create, FileAccess.Write))
            {
                _document.Save(fileStream);
            }
        }

        private static string ReadTemplate(string name)
        {
            var resourceStream = typeof(ProjectFile).GetTypeInfo().Assembly.GetManifestResourceStream($"Dotnet.Script.DependencyModel.ProjectSystem.{name}");
            using (var streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}