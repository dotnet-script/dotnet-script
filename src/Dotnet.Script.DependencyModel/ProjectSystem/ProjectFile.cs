using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    /// <summary>
    /// Represents the subset of a MSBuild project file we need to resolve dependencies.
    /// </summary>
    public class ProjectFile : IEquatable<ProjectFile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFile"/> class.
        /// </summary>
        public ProjectFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFile"/> class
        /// based on the given <paramref name="xmlContent"/>.
        /// /// </summary>
        /// <param name="xmlContent">The contents of a MSBuild project file.</param>
        public ProjectFile(string xmlContent)
        {
            var projectFileDocument = XDocument.Parse(xmlContent);
            var packageReferenceElements = projectFileDocument.Descendants("PackageReference");
            foreach (var packageReferenceElement in packageReferenceElements)
            {
                PackageReferences.Add(new PackageReference(packageReferenceElement.Attribute("Include").Value,packageReferenceElement.Attribute("Version").Value));
            }

            var assemblyReferenceElements = projectFileDocument.Descendants("Reference");
            foreach (var assemblyReference in assemblyReferenceElements)
            {
                AssemblyReferences.Add(new AssemblyReference(assemblyReference.Attribute("Include").Value));
            }
        }

        /// <summary>
        /// Gets a <see cref="bool"/> value that indicates whether this <see cref="ProjectFile"/> can be cached.
        /// </summary>
        public bool IsCacheable { get => PackageReferences.All(reference => reference.IsPinned); }

        /// <summary>
        /// Gets a list of <see cref="PackageReference"/> elements for this <see cref="ProjectFile"/>.
        /// </summary>
        public HashSet<PackageReference> PackageReferences { get ;} = new HashSet<PackageReference>();

        /// <summary>
        /// Gets a list of <see cref="AssemblyReference"/> elements for this <see cref="ProjectFile"/>.
        /// </summary>
        public HashSet<AssemblyReference> AssemblyReferences { get; } = new HashSet<AssemblyReference>();

        /// <summary>
        /// Gets or sets the target framework for this <see cref="ProjectFile"/>.
        /// </summary>
        public string TargetFramework { get; set;} = ScriptEnvironment.Default.TargetFramework;

        public void Save(string pathToProjectFile)
        {
            var projectFileDocument = XDocument.Parse(ReadTemplate("csproj.template"));
            var itemGroupElement = projectFileDocument.Descendants("ItemGroup").Single();
            foreach (var packageReference in PackageReferences)
            {
                var packageReferenceElement = new XElement("PackageReference");
                packageReferenceElement.Add(new XAttribute("Include", packageReference.Id));
                packageReferenceElement.Add(new XAttribute("Version", packageReference.Version));
                itemGroupElement.Add(packageReferenceElement);
            }

            foreach (var assemblyReference in AssemblyReferences)
            {
                var assemblyReferenceElement = new XElement("Reference");
                assemblyReferenceElement.Add(new XAttribute("Include", assemblyReference.AssemblyPath));
                itemGroupElement.Add(assemblyReferenceElement);
            }

            var targetFrameworkElement = projectFileDocument.Descendants("TargetFramework").Single();
            targetFrameworkElement.Value = TargetFramework;

            using (var fileStream = new FileStream(pathToProjectFile, FileMode.Create, FileAccess.Write))
            {
                projectFileDocument.Save(fileStream);
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

        /// <inheritdoc/>
        public bool Equals(ProjectFile other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return PackageReferences.SequenceEqual(other.PackageReferences)
                && AssemblyReferences.SequenceEqual(other.AssemblyReferences)
                && TargetFramework.Equals(other.TargetFramework);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals((ProjectFile)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // We are dealing with a mutable object here so we just return the base implementation.
            return base.GetHashCode();
        }
    }
}