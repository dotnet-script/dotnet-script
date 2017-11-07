using System;
using System.IO;
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

        public NuGetSourceReferenceResolver(SourceReferenceResolver sourceReferenceResolver)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
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
            throw new System.NotImplementedException();
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            if (path.StartsWith("nuget", StringComparison.OrdinalIgnoreCase))
            {
                
            }
            var resolvedReference = _sourceReferenceResolver.ResolveReference(path, baseFilePath);
            return resolvedReference;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            throw new System.NotImplementedException();
        }
    }
}