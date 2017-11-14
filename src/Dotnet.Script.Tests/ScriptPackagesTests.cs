using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Environment;
using Xunit;

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

        [Fact]
        public void ShouldHandleScriptWithAnyTargetFramework()
        {
            var result = Execute("WithAnyTargetFramework/WithAnyTargetFramework.csx");
            Assert.StartsWith("Hello from any target framework", result);
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
                var exitCode = Program.Main(new[] {fullPathToScriptFile});                
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
            Console.WriteLine("TEST");
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
            string pathToPackagesOutputFolder = GetPathToPackagesFolder();           
            RemoveDirectory(pathToPackagesOutputFolder);
            Directory.CreateDirectory(pathToPackagesOutputFolder);
            var specFiles = GetSpecFiles();
            foreach (var specFile in specFiles)
            {
                string command;
                if (RuntimeHelper.IsWindows())
                {
                    command = "nuget";                    
                }
                else
                {
                    command = ProcessHelper.RunAndCaptureOutput("which", new string[]{"nuget"}).output;                
                }
                var result = ProcessHelper.RunAndCaptureOutput(command, new[] { $"pack {specFile}", $"-OutputDirectory {pathToPackagesOutputFolder}" });                                        
            }
        }

        private static string GetPathToPackagesFolder()
        {
            var targetDirectory =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TestFixtures","ScriptPackage","packages");

            var tempDirectory = Path.GetTempPath();
            var pathRoot = Path.GetPathRoot(targetDirectory);
            var targetDirectoryWithoutRoot = targetDirectory.Substring(pathRoot.Length);
            if (pathRoot.Length > 0 && RuntimeHelper.IsWindows())
            {
                var driveLetter = pathRoot.Substring(0, 1);
                if (driveLetter == "\\")
                {
                    targetDirectoryWithoutRoot = targetDirectoryWithoutRoot.TrimStart(new char[] { '\\' });
                    driveLetter = "UNC";
                }

                targetDirectoryWithoutRoot = Path.Combine(driveLetter, targetDirectoryWithoutRoot);
            }
            var pathToProjectDirectory = Path.Combine(tempDirectory, "scripts", targetDirectoryWithoutRoot);

            if (!Directory.Exists(pathToProjectDirectory))
            {
                Directory.CreateDirectory(pathToProjectDirectory);
            }

            return pathToProjectDirectory;            
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