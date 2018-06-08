using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.Tests
{
    public class ScriptPackagesFixture
    {
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptPackagesFixture()
        {
            _scriptEnvironment = ScriptEnvironment.Default;
            ClearGlobalPackagesFolder();
            BuildScriptPackages();            
        }

        private void ClearGlobalPackagesFolder()
        {
            var pathToGlobalPackagesFolder = GetPathToGlobalPackagesFolder();            
            var scriptPackageFolders = Directory.GetDirectories(pathToGlobalPackagesFolder).Select(f => f.ToLower()).Where(f => f.Contains("scriptpackage"));            
            foreach (var scriptPackageFolder in scriptPackageFolders)
            {
                RemoveDirectory(scriptPackageFolder);
            }
        }

        private void BuildScriptPackages()
        {
            string pathToPackagesOutputFolder = GetPathToPackagesFolder();
            RemoveDirectory(pathToPackagesOutputFolder);
            Directory.CreateDirectory(pathToPackagesOutputFolder);
            var specFiles = GetSpecFiles();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pathtoNuget430 = Path.Combine("../../../../Dotnet.Script.DependencyModel/NuGet/NuGet430.exe");
            foreach (var specFile in specFiles)
            {
                string command;
                if (_scriptEnvironment.IsWindows)
                {                    
                    command = pathtoNuget430;
                    var result = ProcessHelper.RunAndCaptureOutput(command, new[] { $"pack {specFile}", $"-OutputDirectory {pathToPackagesOutputFolder}" });
                }
                else
                {
                    command = "mono"; 
                    var result = ProcessHelper.RunAndCaptureOutput(command, new[] { $"{pathtoNuget430} pack {specFile}", $"-OutputDirectory {pathToPackagesOutputFolder}" });
                }
                
            }
        }

        private static string GetPathToPackagesFolder()
        {
            var targetDirectory = TestPathUtils.GetPathToTestFixtureFolder(Path.Combine("ScriptPackage", "packages"));
            return DependencyModel.ProjectSystem.FileUtils.CreateTempFolder(targetDirectory);
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