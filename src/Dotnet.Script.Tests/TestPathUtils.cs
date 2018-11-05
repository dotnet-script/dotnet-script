using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.Tests
{
    public class TestPathUtils
    {
        public static string GetPathToTestFixtureFolder(string fixture)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "TestFixtures", fixture);
        }

        public static string GetPathToTempFolder(string path)
        {
            return DependencyModel.ProjectSystem.FileUtils.GetPathToTempFolder(path);
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
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", "nuget locals global-packages --list");
            var match = Regex.Match(result.output, @"^.*global-packages:\s*(.*)$");
            return match.Groups[1].Value;
        }

        public static void RemovePackageFromGlobalNugetCache(string packageName)
        {
            var pathToGlobalPackagesFolder = TestPathUtils.GetPathToGlobalPackagesFolder();
            var pathToAutoMapperPackage = Directory.GetDirectories(pathToGlobalPackagesFolder).Single(d => d.Contains(packageName, StringComparison.OrdinalIgnoreCase));
            FileUtils.RemoveDirectory(pathToAutoMapperPackage);
        }
    }
}