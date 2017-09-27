using System;
using System.Collections.Generic;
using System.IO;
using Dotnet.Script.DependencyModel.Environment;

namespace Dotnet.Script.DependencyModel.Runtime
{
    /// <summary>
    /// Represents a class that is capable of resolving 
    /// the full path to a dependency specified with a relative path.
    /// </summary>
    public class DependencyPathResolver
    {
        private readonly Action<bool, string> _logger;
        private readonly Lazy<string[]> _possibleNuGetRootLocations = new Lazy<string[]>();

        public DependencyPathResolver(Action<bool, string> logger)
        {
            _logger = logger;
            _possibleNuGetRootLocations = new Lazy<string[]>(ResolvePossibleNugetRootLocations);
        }
        /// <summary>
        /// Gets the full path of a dependency specified with a relative path.
        /// </summary>
        /// <param name="relativePath">The relative of the dependency to resolve.</param>
        /// <returns>The full path of the dependency if resolvable, otherwise null.</returns>
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