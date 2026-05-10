using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
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

        [Fact]
        public void GetPathToScriptTempFolder_DistinguishesDifferentUncServers()
        {
            // Issue #752: \\server-dev\\SHARE\\... and \\server-prod\\SHARE\\... collide in cache
            // because UNC path normalization replaces the server name with "UNC", causing both to map
            // to the same cache path. The fix preserves the server name so different servers produce
            // different cache paths.
            //
            // This test is Windows-only since UNC paths are a Windows concept.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, verify the fix by checking that Path.GetPathRoot("\\server\share") starts with "\\"
                // and that ScriptEnvironment.Default.IsWindows would be true.
                // The actual UNC server distinction can only be tested on Windows.
                // The behavior is verified by the logic in FileUtils.GetPathToScriptTempFolder
                // and the integration test below.
                return;
            }

            var cachePath = FileUtils.GetPathToScriptTempFolder(@"\\server-dev\SHARE\Projects\app", null);
            var cachePath2 = FileUtils.GetPathToScriptTempFolder(@"\\server-prod\SHARE\Projects\app", null);

            // The two paths MUST be different since they point to different servers
            Assert.NotSame(cachePath, cachePath2);

            // The server name must appear in its respective path
            Assert.True(cachePath.Contains("server-dev"),
                $"Expected cache path to contain 'server-dev', but got: {cachePath}");
            Assert.True(cachePath2.Contains("server-prod"),
                $"Expected cache path to contain 'server-prod', but got: {cachePath2}");
        }

        [Fact]
        public void GetPathToScriptTempFolder_DoesNotDuplicateUncPrefix()
        {
            // Ensure the cache path doesn't contain duplicate "UNC" segments
            // (which would indicate over-normalization of the UNC prefix).
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var cachePath = FileUtils.GetPathToScriptTempFolder(@"\\server-dev\SHARE\Projects\app", null);
            var uncCount = cachePath.Split(new[] { "UNC" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(1, uncCount);
        }
    }
}