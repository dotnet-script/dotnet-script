using System;
using System.IO;

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
            return pathToFixture;
        }
    }
}