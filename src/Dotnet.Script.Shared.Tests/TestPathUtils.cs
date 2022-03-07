using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dotnet.Script.Shared.Tests
{
    public class TestPathUtils
    {
        public static string GetPathToTestFixtureFolder(string fixture)
        {

            var baseDirectory = Path.GetDirectoryName(new Uri(typeof(TestPathUtils).Assembly.CodeBase).LocalPath);
            return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "Dotnet.Script.Tests", "TestFixtures", fixture));
        }

        public static string GetPathToTempFolder(string path)
        {
            return DependencyModel.ProjectSystem.FileUtils.GetPathToScriptTempFolder(path);
        }

        public static string GetPathToScriptPackages(string fixture)
        {
            var pathToTestFixtureFolder = GetPathToTestFixtureFolder(fixture);
            return Path.Combine(GetPathToTempFolder(pathToTestFixtureFolder), "..", "packages");
        }

        public static string GetPathToTestFixture(string fixture)
        {
            var fixtureFolderPath = GetPathToTestFixtureFolder(fixture);
            var pathToFixture = Path.Combine(fixtureFolderPath, $"{Path.GetFileNameWithoutExtension(fixtureFolderPath)}.csx");
            return Path.GetFullPath(pathToFixture);
        }

        public static string GetPathToGlobalPackagesFolder()
        {
            var (output, _) = ProcessHelper.RunAndCaptureOutput("dotnet", "nuget locals global-packages --list");
            var match = Regex.Match(output, @"^.*global-packages:\s*(.*)$");
            return match.Groups[1].Value;
        }

        public static void RemovePackageFromGlobalNugetCache(string packageName)
        {
            var pathToGlobalPackagesFolder = GetPathToGlobalPackagesFolder();
            var pathToPackage = Directory.GetDirectories(pathToGlobalPackagesFolder).SingleOrDefault(d => d.Contains(packageName, StringComparison.OrdinalIgnoreCase));
            if (pathToPackage != null)
            {
                FileUtils.RemoveDirectory(pathToPackage);
            }
        }
    }
}