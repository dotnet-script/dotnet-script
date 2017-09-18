using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Process;

namespace Dotnet.Script.DependencyModel
{
    public class DependencyPathResolver : IDependencyPathResolver
    {
        private readonly Action<bool, string> _logger;
        private readonly Lazy<string[]> _possibleNuGetRootLocations = new Lazy<string[]>();

        public DependencyPathResolver(Action<bool, string> logger)
        {
            _logger = logger;
            _possibleNuGetRootLocations = new Lazy<string[]>(ResolvePossibleNugetRootLocations);
        }

        public string GetFullPath(string relativePath)
        {
            foreach (var possibleLocation in _possibleNuGetRootLocations.Value)
            {
                var fullPath = Path.Combine(possibleLocation, relativePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            throw new InvalidOperationException("Not found");
        }

        private string[] ResolvePossibleNugetRootLocations()
        {
            List<string> result = new List<string>();
            result.Add(GetPathToGlobalPackagesFolder());
            if (RuntimeHelper.IsWindows())
            {
                var programFilesFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
                var processArchitecture = RuntimeHelper.GetProcessArchitecture();
                var storePath = Path.Combine(programFilesFolder, "dotnet", "store", processArchitecture, "netcoreapp2.0");
                result.Add(storePath);
            }
            return result.ToArray();
        }

        private string GetPathToGlobalPackagesFolder()
        {
            var result = Command.Execute("dotnet", "nuget locals global-packages -l", _logger);
            var match = Regex.Match(result, @"global-packages:\s*(.*)");
            var pathToGlobalPackagesFolder = match.Groups[1].Captures[0].ToString();
            return pathToGlobalPackagesFolder.Replace("\r", String.Empty);
        }
    }
}