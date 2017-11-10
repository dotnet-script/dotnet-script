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
        private static Regex capturePackageName = new Regex(@"\s*nuget\s*:\s*(.*)\s*,", RegexOptions.Compiled | RegexOptions.IgnoreCase); 

        public NuGetSourceReferenceResolver(SourceReferenceResolver sourceReferenceResolver, IDictionary<string, IReadOnlyList<string>> scriptMap)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
            _scriptMap = scriptMap;
        }

        
        public override bool Equals(object other)
        {
            return _sourceReferenceResolver.Equals(other);
        }

        public override int GetHashCode()
        {
            return _sourceReferenceResolver.GetHashCode();
        }

        public override string NormalizePath(string path, string baseFilePath)
        {
            return _sourceReferenceResolver.NormalizePath(path, baseFilePath);
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            if (path.StartsWith("nuget", StringComparison.OrdinalIgnoreCase))
            {
                var packageName = capturePackageName.Match(path).Groups[1].Value;
                var scripts = _scriptMap[packageName];
                if (scripts.Count == 1)
                {
                    return scripts[0];
                }
            }
            var resolvedReference = _sourceReferenceResolver.ResolveReference(path, baseFilePath);
            return resolvedReference;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            return _sourceReferenceResolver.OpenRead(resolvedPath);            
        }
    }
}