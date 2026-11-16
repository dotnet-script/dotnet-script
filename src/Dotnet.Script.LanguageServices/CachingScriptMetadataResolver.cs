using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// Resolving a reference hits the file system and, for NuGet references, the package cache.
    /// The REPL asks for the same references over and over, so the results are memoized. One instance is
    /// kept per reference set, which is also what bounds the cache: it goes away when references change.
    /// </summary>
    internal class CachingScriptMetadataResolver : MetadataReferenceResolver
    {
        private readonly ConcurrentDictionary<(string Reference, string BaseFilePath, MetadataReferenceProperties Properties), ImmutableArray<PortableExecutableReference>> _directReferences =
            new ConcurrentDictionary<(string, string, MetadataReferenceProperties), ImmutableArray<PortableExecutableReference>>();

        private readonly ConcurrentDictionary<string, PortableExecutableReference> _missingReferences =
            new ConcurrentDictionary<string, PortableExecutableReference>();

        private readonly MetadataReferenceResolver _resolver;

        public CachingScriptMetadataResolver(MetadataReferenceResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public override bool Equals(object other) =>
            other is CachingScriptMetadataResolver resolver && _resolver.Equals(resolver._resolver);

        public override int GetHashCode() => _resolver.GetHashCode();

        public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) =>
            _missingReferences.GetOrAdd(
                referenceIdentity.GetDisplayName(),
                _ => _resolver.ResolveMissingAssembly(definition, referenceIdentity));

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties) =>
            _directReferences.GetOrAdd(
                (reference, baseFilePath, properties),
                key => _resolver.ResolveReference(key.Reference, key.BaseFilePath, key.Properties));
    }
}
