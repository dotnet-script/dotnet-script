using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Compilation
{
    /// <summary>
    ///
    /// </summary>
    public class CompilationReferencesReader : ICompilationReferenceReader
    {
        private readonly CommandRunner _commandRunner;
        private Logger _log;

        public CompilationReferencesReader(LogFactory logFactory) : this(new CommandRunner(logFactory), logFactory)
        {
        }

        public CompilationReferencesReader(CommandRunner commandRunner, LogFactory logFactory)
        {
            _log = logFactory.CreateLogger<CompilationReferencesReader>();
            _commandRunner = commandRunner;
        }

        public IEnumerable<CompilationReference> Read(ProjectFileInfo projectFile)
        {
            const string outputDirectory = "compilation";
            var workingDirectory = Path.GetDirectoryName(projectFile.Path);
            if (!Directory.Exists(Path.Combine(workingDirectory, outputDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(workingDirectory, outputDirectory));
            }
            // Copy the csproj file so that we don't interfere with the project.assets.json file
            // used for execution.
            string pathToCompilationProjectFile = Path.Combine(workingDirectory, outputDirectory, Path.GetFileName(projectFile.Path));
            File.Copy(projectFile.Path, pathToCompilationProjectFile, true);

            // We remove any third party package references since we are only after the framework assemblies here.
            RemovePackageReferences(pathToCompilationProjectFile);

            var referencePathsFile = Path.Combine(workingDirectory, outputDirectory, "ReferencePaths.txt");
            if (File.Exists(referencePathsFile))
            {
                File.Delete(referencePathsFile);
            }
            var exitCode = _commandRunner.Execute("dotnet", $"build \"{pathToCompilationProjectFile}\" /p:OutputType=Library -o {outputDirectory} --nologo", workingDirectory);
            if (exitCode != 0)
            {
                throw new Exception($"Unable to read compilation dependencies for '{projectFile.Path}'. Make sure that all script files contains valid NuGet references");
            }
            var referenceAssemblies = File.ReadAllLines(referencePathsFile);
            var compilationReferences = referenceAssemblies.Select(ra => new CompilationReference(ra)).ToArray();
            return compilationReferences;
        }

        private static void RemovePackageReferences(string projectFile)
        {
            var document = XDocument.Load(projectFile);
            document.Descendants("PackageReference").Remove();
            document.Save(projectFile);
        }
    }
}