using System;
using System.Collections.Generic;
using System.IO;
using Dotnet.Script.DependencyModel.Environment;

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
            var possibleNuGetRootLocations = new List<string>();
            possibleNuGetRootLocations.Add(RuntimeHelper.GetPathToGlobalPackagesFolder());
            possibleNuGetRootLocations.Add(RuntimeHelper.GetPathToNuGetStoreFolder());            
            return possibleNuGetRootLocations.ToArray();
        }

       
    }
}