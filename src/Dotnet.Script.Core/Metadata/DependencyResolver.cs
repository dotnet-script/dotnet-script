using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dotnet.Script.Core.Internal;
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
            string[] possibleNuGetRootLocations = ResolvePossibleNugetRootLocation();

            
            var runtimeDepedencies = new HashSet<RuntimeDependency>();

            var context = ReadDependencyContext(pathToProjectFile);

            //Note: Scripting only releates to runtime libraries.
            var runtimeLibraries = context.RuntimeLibraries;
            try
            {
                foreach (var runtimeLibrary in runtimeLibraries)
                {
                    ProcessNativeLibraries(runtimeLibrary, possibleNuGetRootLocations);
                    ProcessRuntimeAssemblies(runtimeLibrary, possibleNuGetRootLocations, runtimeDepedencies);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            

            return runtimeDepedencies;
        }

        private void ProcessRuntimeAssemblies(RuntimeLibrary runtimeLibrary, string[] possibleNuGetRootLocations,
            HashSet<RuntimeDependency> runtimeDepedencies)
        {
            if (runtimeLibrary.Name.ToLower().Contains("sqlite"))
            {
                
            }
            foreach (var runtimeAssemblyGroup in runtimeLibrary.RuntimeAssemblyGroups.Where(rag => IsRelevantForCurrentRuntime(rag.Runtime)))
            {
                foreach (var assetPath in runtimeAssemblyGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    if (!path.EndsWith("_._"))
                    {
                        var fullPath = ResolveFullPathToDependency(path, possibleNuGetRootLocations);
                            
                        //var fullPath = Path.Combine(pathToGlobalPackagesFolder, path);
                        _logger.Verbose(fullPath);
                        runtimeDepedencies.Add(new RuntimeDependency(runtimeLibrary.Name, fullPath));
                    }
                }
            }
        }

        private string ResolveFullPathToDependency(string path, string[] possibleLocations)
        {
            foreach (var possibleLocation in possibleLocations)
            {
                var fullPath = Path.Combine(possibleLocation, path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }                
            }
            throw new InvalidOperationException("Not found");

        }

        private void ProcessNativeLibraries(RuntimeLibrary runtimeLibrary, string[] possibleNuGetRootLocations)
        {
            foreach (var nativeLibraryGroup in runtimeLibrary.NativeLibraryGroups.Where(nlg => IsRelevantForCurrentRuntime(nlg.Runtime)))
            {
                foreach (var assetPath in nativeLibraryGroup.AssetPaths)
                {
                    var path = Path.Combine(runtimeLibrary.Path, assetPath);
                    var fullPath = ResolveFullPathToDependency(path,
                        possibleNuGetRootLocations);

                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
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
            var runtimeId = RuntimeHelper.GetRuntimeIdentifier();
            _commandRunner.Execute("dotnet", $"restore {pathToProjectFile} -r {runtimeId}");
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
            return string.IsNullOrWhiteSpace(runtime) || runtime == RuntimeHelper.GetRuntimeIdentifier();
        }

        private string[] ResolvePossibleNugetRootLocation()
        {
            List<string> result = new List<string>(); 
            result.Add(GetPathToGlobalPackagesFolder());
            if (RuntimeHelper.GetPlatformIdentifier() == "win")
            {
                var programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var processArchitecture = RuntimeHelper.GetProcessArchitecture();                
                var storePath = Path.Combine(programFilesFolder, "dotnet", "store", processArchitecture, "netcoreapp2.0");

                result.Add(storePath);
            }
            return result.ToArray();
        }
    }
}