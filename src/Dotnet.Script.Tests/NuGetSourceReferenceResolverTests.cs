using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Dotnet.Script.DependencyModel.NuGet;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Dotnet.Script.Tests
{
    [Collection("IntegrationTests")]
    public class NuGetSourceReferenceResolverTests
    {
        [Fact]
        public void ShouldHandleResolvingInvalidPackageReference()
        {
            Dictionary<string, IReadOnlyList<string>> scriptMap = new Dictionary<string, IReadOnlyList<string>>();
            NuGetSourceReferenceResolver resolver = new NuGetSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, Directory.GetCurrentDirectory()), scriptMap);
            resolver.ResolveReference("nuget:InvalidPackage, 1.2.3", Directory.GetCurrentDirectory());
        }
    }
}
