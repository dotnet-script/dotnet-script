using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.Shared.Tests;

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
            var pathToGlobalPackagesFolder = TestPathUtils.GetPathToGlobalPackagesFolder();
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
            foreach (var specFile in specFiles)
            {
                var result  = ProcessHelper.RunAndCaptureOutput("dotnet", $"pack \"{specFile}\" -o \"{pathToPackagesOutputFolder}\"");
                if (result.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to pack {specFile}: {result.Output}");
                }
            }
        }

        internal static string GetPathToPackagesFolder()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "obj", "packages");
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

        private static IReadOnlyList<string> GetSpecFiles()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pathToScriptPackages = Path.Combine(baseDirectory, "..", "..", "..", "ScriptPackages");
            // The csproj files contains the nuspec references
            return Directory.GetFiles(pathToScriptPackages, "*.csproj", SearchOption.AllDirectories);
        }
    }
}