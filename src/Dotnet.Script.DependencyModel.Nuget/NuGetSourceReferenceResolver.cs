using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.DependencyModel.NuGet
{
    /// <summary>
    /// A <see cref="SourceReferenceResolver"/> decorator that handles
    /// references to NuGet packages in scripts.  
    /// </summary>
    public class NuGetSourceReferenceResolver : SourceReferenceResolver
    {
        private readonly SourceReferenceResolver _sourceReferenceResolver;
        private readonly IDictionary<string, IReadOnlyList<string>> _scriptMap;
        private static readonly Regex PackageNameMatcher = new Regex(@"\s*nuget\s*:\s*(.*)\s*,", RegexOptions.Compiled | RegexOptions.IgnoreCase); 

        public NuGetSourceReferenceResolver(SourceReferenceResolver sourceReferenceResolver, IDictionary<string, IReadOnlyList<string>> scriptMap)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
            _scriptMap = scriptMap;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return _sourceReferenceResolver.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _sourceReferenceResolver.GetHashCode();
        }

        public override string NormalizePath(string path, string baseFilePath)
        {
            if (path.StartsWith("nuget", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return _sourceReferenceResolver.NormalizePath(path, baseFilePath);
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            if (path.StartsWith("nuget", StringComparison.OrdinalIgnoreCase))
            {
                var packageName = PackageNameMatcher.Match(path).Groups[1].Value;
                var scripts = _scriptMap[packageName];
                if (scripts.Count == 1)
                {
                    return scripts[0];
                }
                return path;
            }
            var resolvedReference = _sourceReferenceResolver.ResolveReference(path, baseFilePath);
            return resolvedReference;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            if (resolvedPath.StartsWith("nuget", StringComparison.OrdinalIgnoreCase))
            {
                var packageName = PackageNameMatcher.Match(resolvedPath).Groups[1].Value;
                var scripts = _scriptMap[packageName];
                if (scripts.Count == 1)
                {
                    return _sourceReferenceResolver.OpenRead(resolvedPath);
                }
                if (scripts.Count > 1)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    StreamWriter streamWriter = new StreamWriter(memoryStream);
                    foreach (var script in scripts)
                    {
                        var loadStatement = $"#load \"{script}\"";
                        streamWriter.WriteLine(loadStatement);
                    }
                    streamWriter.Flush();
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }

                return _sourceReferenceResolver.OpenRead(resolvedPath);            
        }
    }
}