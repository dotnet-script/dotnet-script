using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Dotnet.Script.DependencyModel.ProjectSystem;

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

        public NuGetSourceReferenceResolver(SourceReferenceResolver sourceReferenceResolver, IDictionary<string, IReadOnlyList<string>> scriptMap)
        {
            _sourceReferenceResolver = sourceReferenceResolver;
            _scriptMap = scriptMap;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
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
            if (path.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return _sourceReferenceResolver.NormalizePath(path, baseFilePath);
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            if (ScriptParser.TryParseNuGetPackageReference(path, out var packageName, out _))
            {
                if (_scriptMap.TryGetValue(packageName, out var scripts))
                {
                    if (scripts.Count == 1)
                    {
                        return scripts[0];
                    }
                    return path;
                }
            }
            var resolvedReference = _sourceReferenceResolver.ResolveReference(path, baseFilePath);
            return resolvedReference;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            if (ScriptParser.TryParseNuGetPackageReference(resolvedPath, out var packageName, out _))
            {
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