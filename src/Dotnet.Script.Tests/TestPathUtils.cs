using System;
using System.IO;

namespace Dotnet.Script.Tests
{
    public class TestPathUtils
    {
        public static string GetFullPathToTestFixture(string path)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "TestFixtures", path);
        }
    }
}