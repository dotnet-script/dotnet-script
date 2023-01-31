using System;
using System.IO;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class FileUtilsTests
    {
        [Fact]
        public void GetTempPathCanBeOverridenWithAbsolutePathViaEnvVar()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try 
            {
                Environment.SetEnvironmentVariable("DOTNET_SCRIPT_CACHE_LOCATION", path);
                var tempPath = FileUtils.GetTempPath();
                Assert.Equal(path, tempPath);
            } 
            finally 
            {
                Environment.SetEnvironmentVariable("DOTNET_SCRIPT_CACHE_LOCATION", null);
            }
        }

        [Fact]
        public void GetTempPathCanBeOverridenWithRelativePathViaEnvVar()
        {
            var path = "foo";
            try 
            {
                Environment.SetEnvironmentVariable("DOTNET_SCRIPT_CACHE_LOCATION", path);
                var tempPath = FileUtils.GetTempPath();
                Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), path), tempPath);
            } 
            finally 
            {
                Environment.SetEnvironmentVariable("DOTNET_SCRIPT_CACHE_LOCATION", null);
            }
        }
    }
}
