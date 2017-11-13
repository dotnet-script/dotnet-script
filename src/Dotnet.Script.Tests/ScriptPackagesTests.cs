using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet.Script.Tests
{
    [Collection("ScriptPackagesTests")]
    public class ScriptPackagesTests : IClassFixture<ScriptPackagesFixture>
    {
                   
        [Fact]
        public void ShouldHandleScriptPackageWithMainCsx()
        {            
            var result = Execute("WithMainCsx/WithMainCsx.csx");           
            Assert.StartsWith("Hello from netstandard2.0", result);            
        }

             
        private string Execute(string scriptFileName)
        {
            var output = new StringBuilder();
            var stringWriter = new StringWriter(output);
            var oldOut = Console.Out;
            try
            {
                Console.SetOut(stringWriter);                                
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var fullPathToScriptFile = Path.Combine(baseDir, "..", "..", "..", "TestFixtures", "ScriptPackage", scriptFileName);
                Program.Main(new[] {fullPathToScriptFile});                
                return output.ToString();
                
            }
            finally 
            {
                Console.SetOut(oldOut);
            }
        }
    }

    public class ScriptPackagesFixture
    {
        public ScriptPackagesFixture()
        {
            ClearGlobalPackagesFolder();
            BuildScriptPackages();
        }

        private void ClearGlobalPackagesFolder()
        {            
            var pathToGlobalPackagesFolder = GetPathToGlobalPackagesFolder();
            var scriptPackageFolders = Directory.GetDirectories(pathToGlobalPackagesFolder, "ScriptPackage*");
            foreach (var scriptPackageFolder in scriptPackageFolders)
            {
                RemoveDirectory(scriptPackageFolder);
            }
        }

        private static void BuildScriptPackages()
        {
            var tempPath = Path.GetTempPath();
            string pathToPackagesOutputFolder;
            if (RuntimeHelper.IsWindows())
            {
                var driveLetter = tempPath.Substring(0, 1);
                pathToPackagesOutputFolder = Path.Combine(tempPath, "scripts", driveLetter, "packages");
            }
            else
            {
                pathToPackagesOutputFolder = Path.Combine(tempPath, "scripts", "packages");
            }

            RemoveDirectory(pathToPackagesOutputFolder);
            Directory.CreateDirectory(pathToPackagesOutputFolder);
            var specFiles = GetSpecFiles();
            foreach (var specFile in specFiles)
            {
                var command = RuntimeHelper.IsWindows() ? "nuget" : "mono nuget";
                ProcessHelper.RunAndCaptureOutput(command, new[] { $"pack {specFile}", $"-OutputDirectory {pathToPackagesOutputFolder}" });
            }
        }

        private static void RemoveDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            // http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
            foreach (string directory in Directory.GetDirectories(path))
            {
                RemoveDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }


        private string GetPathToGlobalPackagesFolder()
        {
            var result = ProcessHelper.RunAndCaptureOutput("dotnet", new[] { "nuget", "locals", "global-packages", "--list" });
            var match = Regex.Match(result.output, @"^.*global-packages:\s*(.*)$");
            return match.Groups[1].Value;
        }

        private static IReadOnlyList<string> GetSpecFiles()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pathToScriptPackages = Path.Combine(baseDirectory, "..", "..", "..", "ScriptPackages");
            return Directory.GetFiles(pathToScriptPackages, "*.nuspec", SearchOption.AllDirectories);
        }
    }
}