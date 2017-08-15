using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dotnet.Script.Core.Internal;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.Core.Metadata
{
    /// <summary>
    /// An <see cref="IDependencyResolver"/> that resolves runtime dependencies 
    /// from an "csproj" file.
    /// </summary>
    public class DependencyResolver : IDependencyResolver
    {
        private readonly CommandRunner _commandRunner;

        private readonly ScriptLogger _logger;

        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("libdl.so")]
        protected static extern IntPtr dlopen(string filename, int flags);


        public DependencyResolver(CommandRunner commandRunner, ScriptLogger logger)
        {
            _commandRunner = commandRunner;
            _logger = logger;
        }

        private DependencyContext ReadDependencyContext(string pathToProjectFile)
        {
            Restore(pathToProjectFile);

            var pathToAssetsFiles = Path.Combine(Path.GetDirectoryName(pathToProjectFile), "obj", "project.assets.json");

            using (FileStream fs = new FileStream(pathToAssetsFiles, FileMode.Open, FileAccess.Read))
            {
                using (var contextReader = new DependencyContextJsonReader())
                {
                    return contextReader.Read(fs);
                }
            }
        }

        public IEnumerable<RuntimeDependency> GetRuntimeDependencies(string pathToProjectFile)
        {
            var pathToGlobalPackagesFolder = GetPathToGlobalPackagesFolder();
            var runtimeDepedencies = new HashSet<RuntimeDependency>();

            var context = ReadDependencyContext(pathToProjectFile);

            //Note: Scripting only releates to runtime libraries.
            var runtimeLibraries = context.RuntimeLibraries;

            foreach (var runtimeLibrary in runtimeLibraries)
            {
                ProcessNativeLibraries(runtimeLibrary, pathToGlobalPackagesFolder);
                ProcessRuntimeAssemblies(runtimeLibrary, pathToGlobalPackagesFolder, runtimeDepedencies);
            }

            return runtimeDepedencies;
        }

        private void ProcessRuntimeAssemblies(RuntimeLibrary runtimeLibrary, string pathToGlobalPackagesFolder,
            HashSet<RuntimeDependency> runtimeDepedencies)
        {

            foreach (var runtimeAssemblyGroup in runtimeLibrary.RuntimeAssemblyGroups.Where(rag => IsRelevantForCurrentRuntime(rag.Runtime)))
            {
                foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    if (!path.EndsWith("_._"))
                    {
                        var fullPath = Path.Combine(pathToGlobalPackagesFolder, path);
                        _logger.Verbose(fullPath);
                        runtimeDepedencies.Add(new RuntimeDependency(runtimeLibrary.Name, fullPath));
                    }
                }
            }
        }

        private void ProcessNativeLibraries(RuntimeLibrary runtimeLibrary, string pathToGlobalPackagesFolder)
        {
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(nlg => IsRelevantForCurrentRuntime(nlg.Runtime)))
            {
                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var fullPath = Path.Combine(pathToGlobalPackagesFolder, runtimeLibrary.Path,
                        assetPath);
                    _logger.Verbose($"Loading native library from {fullPath}");
                    if (RuntimeHelper.GetPlatformIdentifier() == "win")
                    {
                        LoadLibrary(fullPath);
                    }
                    else
                    {
                        // Maybe something like this?
                        // https://stackoverflow.com/questions/13461989/p-invoke-to-dynamically-loaded-library-on-mono
                    }
                }
            }
        }

        private void Restore(string pathToProjectFile)
        {
            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            _commandRunner.Execute("DotNet", $"restore {pathToProjectFile} -r {runtimeId}");
            //_commandRunner.Execute("DotNet", $"restore {pathToProjectFile}");
        }

        private string GetPathToGlobalPackagesFolder()
        {            
            var result = _commandRunner.Execute("dotnet", "nuget locals global-packages -l");
            var match = Regex.Match(result, @"global-packages:\s*(.*)");
            var pathToGlobalPackagesFolder = match.Groups[1].Captures[0].ToString();
            return pathToGlobalPackagesFolder.Replace("\r", String.Empty);
        }

        public bool IsRelevantForCurrentRuntime(string runtime)
        {
            return string.IsNullOrWhiteSpace(runtime) || runtime == GetRuntimeIdentitifer();
        }

        private static string GetRuntimeIdentitifer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "unix";

            return "win";
        }
    }
}