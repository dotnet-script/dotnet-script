﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Compilation;
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

        [Fact]
        public void ShouldHandleScriptPackageWithNoEntryPointFile()
        {
            var result = Execute("WithNoEntryPointFile/WithNoEntryPointFile.csx");
            Assert.Contains("Hello from Foo.csx", result);
            Assert.Contains("Hello from Bar.csx", result);
        }

        [Fact]
        public void ShouldHandleScriptPackageWithScriptPackageDependency()
        {
            var result = Execute("WithScriptPackageDependency/WithScriptPackageDependency.csx");
            Assert.StartsWith("Hello from netstandard2.0", result);
        }

        [Fact]
        public void ShouldThrowExceptionWhenReferencingUnknownPackage()
        {
            var result = Execute("WithInvalidPackageReference/WithInvalidPackageReference.csx");
            Assert.StartsWith("Unable to restore packages from", result);
        }

        [Fact]
        public void ShouldHandleScriptPackageWithSubFolder()
        {
            var result = Execute("WithSubFolder/WithSubFolder.csx");
            Assert.StartsWith("Hello from Bar.csx", result);
        }

        [Fact]
        public void ShouldGetScriptFilesFromScriptPackage()
        {
            var resolver = CreateResolverCompilationDependencyResolver();
            var fixture = GetFullPathToTestFixture("ScriptPackage/WithMainCsx");
            var dependencies = resolver.GetDependencies(fixture, true, "netcoreapp2.0");
            var scriptFiles = dependencies.Single(d => d.Name == "ScriptPackageWithMainCsx").Scripts;
            Assert.NotEmpty(scriptFiles);
        }

        private static string GetFullPathToTestFixture(string path)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "..", "..", "..", "TestFixtures", path);
        }


        private CompilationDependencyResolver CreateResolverCompilationDependencyResolver()
        {
            var resolver = new CompilationDependencyResolver(type => ((level, message) =>
            {

            }));
            return resolver;
        }

        private string Execute(string scriptFileName)
        {
            var output = new StringBuilder();
            var stringWriter = new StringWriter(output);
            var oldOut = Console.Out;
            var oldErrorOut = Console.Error;
            try
            {
                Console.SetOut(stringWriter);
                Console.SetError(stringWriter);
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var fullPathToScriptFile = Path.Combine(baseDir, "..", "..", "..", "TestFixtures", "ScriptPackage", scriptFileName);
                Program.Main(new[] { fullPathToScriptFile });
                return output.ToString();

            }
            finally
            {
                Console.SetOut(oldOut);
                Console.SetError(oldErrorOut);
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
            var scriptPackageFolders = Directory.GetDirectories(pathToGlobalPackagesFolder).Select(f => f.ToLower()).Where(f => f.Contains("scriptpackage"));            
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
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pathtoNuget430 = Path.Combine("../../../../Dotnet.Script.DependencyModel/NuGet/NuGet430.exe");
            foreach (var specFile in specFiles)
            {
                string command;
                if (RuntimeHelper.IsWindows())
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
            var targetDirectory = TestPathUtils.GetFullPathToTestFixture(Path.Combine("ScriptPackage", "packages"));
            return RuntimeHelper.CreateTempFolder(targetDirectory);
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