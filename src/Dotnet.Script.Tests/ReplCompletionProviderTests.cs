using System;
using System.IO;
using System.Linq;
using Dotnet.Script.Core.Interactive.LineEditing;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class ReplCompletionProviderTests
    {
        private class Outer
        {
            public string Inner { get; set; }
        }

        private static CompletionResult Complete(ReplCompletionProvider provider, string text) =>
            provider.GetCompletions(text, text.Length);

        [Fact]
        public void ShouldCompleteDirectiveNames()
        {
            var result = Complete(new ReplCompletionProvider(), "#lo");

            Assert.Equal(0, result.Start);
            Assert.Equal(3, result.Length);
            Assert.Equal(new[] { "#load" }, result.Items);
        }

        [Fact]
        public void ShouldCompleteAllDirectivesForABareHash()
        {
            var result = Complete(new ReplCompletionProvider(), "#");

            Assert.Contains("#exit", result.Items);
            Assert.Contains("#reset", result.Items);
            Assert.Contains("#r", result.Items);
        }

        [Fact]
        public void ShouldCompleteScriptFilesForLoadDirective()
        {
            using var folder = new DisposableFolder();
            File.WriteAllText(Path.Combine(folder.Path, "helpers.csx"), string.Empty);
            File.WriteAllText(Path.Combine(folder.Path, "library.dll"), string.Empty);
            Directory.CreateDirectory(Path.Combine(folder.Path, "scripts"));

            var provider = new ReplCompletionProvider(currentDirectory: () => folder.Path);
            var result = Complete(provider, "#load \"");

            Assert.Equal(new[] { "helpers.csx", "scripts/" }, result.Items.OrderBy(i => i).ToArray());
        }

        [Fact]
        public void ShouldCompleteAssembliesForReferenceDirective()
        {
            using var folder = new DisposableFolder();
            File.WriteAllText(Path.Combine(folder.Path, "helpers.csx"), string.Empty);
            File.WriteAllText(Path.Combine(folder.Path, "library.dll"), string.Empty);

            var provider = new ReplCompletionProvider(currentDirectory: () => folder.Path);
            var result = Complete(provider, "#r \"lib");

            Assert.Equal(new[] { "library.dll" }, result.Items);
            Assert.Equal(4, result.Start);
            Assert.Equal(3, result.Length);
        }

        [Fact]
        public void ShouldCompletePathsInsideSubdirectories()
        {
            using var folder = new DisposableFolder();
            Directory.CreateDirectory(Path.Combine(folder.Path, "scripts"));
            File.WriteAllText(Path.Combine(folder.Path, "scripts", "nested.csx"), string.Empty);

            var provider = new ReplCompletionProvider(currentDirectory: () => folder.Path);
            var result = Complete(provider, "#load \"scripts/ne");

            Assert.Equal(new[] { "scripts/nested.csx" }, result.Items);
        }

        [Fact]
        public void ShouldNotCompleteNuGetReferences()
        {
            var provider = new ReplCompletionProvider(currentDirectory: Directory.GetCurrentDirectory);

            Assert.True(Complete(provider, "#r \"nuget: Auto").IsEmpty);
        }

        [Fact]
        public void ShouldNotCompleteInsideAClosedQuote()
        {
            var provider = new ReplCompletionProvider(currentDirectory: Directory.GetCurrentDirectory);

            Assert.True(Complete(provider, "#load \"script.csx\"").IsEmpty);
        }

        [Fact]
        public void ShouldReturnNothingForAMissingDirectory()
        {
            var provider = new ReplCompletionProvider(currentDirectory: () => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            Assert.True(Complete(provider, "#load \"").IsEmpty);
        }

        [Fact]
        public void ShouldCompleteScriptVariables()
        {
            var provider = new ReplCompletionProvider(() => new[] { "answer", "another" });
            var result = Complete(provider, "ans");

            Assert.Equal(new[] { "answer" }, result.Items);
            Assert.Equal(0, result.Start);
            Assert.Equal(3, result.Length);
        }

        [Fact]
        public void ShouldCompleteKeywords()
        {
            var result = Complete(new ReplCompletionProvider(), "fore");

            Assert.Contains("foreach", result.Items);
        }

        [Fact]
        public void ShouldSurviveAFailingVariableSource()
        {
            var provider = new ReplCompletionProvider(() => throw new InvalidOperationException("faulted"));

            Assert.Contains("void", Complete(provider, "voi").Items);
        }

        [Fact]
        public void ShouldCompleteMembersOfAVariable()
        {
            var provider = new ReplCompletionProvider(variableTypeResolver: name => name == "s" ? typeof(string) : null);
            var result = Complete(provider, "s.Sub");

            Assert.Equal(new[] { "Substring" }, result.Items);
            Assert.Equal(2, result.Start);
            Assert.Equal(3, result.Length);
        }

        [Fact]
        public void ShouldNotOfferPropertyAccessors()
        {
            var provider = new ReplCompletionProvider(variableTypeResolver: name => typeof(string));
            var items = Complete(provider, "s.").Items;

            Assert.Contains("Length", items);
            Assert.DoesNotContain("get_Length", items);
        }

        [Fact]
        public void ShouldWalkAMemberChain()
        {
            var provider = new ReplCompletionProvider(variableTypeResolver: name => name == "x" ? typeof(Outer) : null);
            var result = Complete(provider, "x.Inner.Substr");

            Assert.Equal(new[] { "Substring" }, result.Items);
        }

        [Fact]
        public void ShouldResolveWellKnownTypeNames()
        {
            var result = Complete(new ReplCompletionProvider(), "Console.Write");

            Assert.Contains("WriteLine", result.Items);
        }

        [Fact]
        public void ShouldReturnNothingForAnUnresolvableChain()
        {
            var provider = new ReplCompletionProvider(variableTypeResolver: name => null);

            Assert.True(Complete(provider, "NoSuchThing.Mem").IsEmpty);
        }

        [Fact]
        public void ShouldReturnNothingWhenTheChainIsNotAPlainMemberAccess()
        {
            var provider = new ReplCompletionProvider(variableTypeResolver: name => typeof(string));

            Assert.True(Complete(provider, "Foo().Bar").IsEmpty);
        }

        [Fact]
        public void ShouldCompleteOnTheCurrentLineOfAMultiLineSubmission()
        {
            var provider = new ReplCompletionProvider(() => new[] { "answer" });
            const string text = "var x = 1;\nans";

            var result = provider.GetCompletions(text, text.Length);

            Assert.Equal(new[] { "answer" }, result.Items);
            Assert.Equal(11, result.Start);
        }
    }
}
