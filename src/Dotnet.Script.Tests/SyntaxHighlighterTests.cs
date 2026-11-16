using System.Linq;
using Dotnet.Script.Core.Interactive.LineEditing;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class SyntaxHighlighterTests
    {
        private readonly SyntaxHighlighter _highlighter = new SyntaxHighlighter();

        [Fact]
        public void ShouldClassifyContextualKeyword()
        {
            var spans = _highlighter.Classify("var x = 1;");

            Assert.Contains(spans, s => s.Kind == ClassificationKind.Keyword && s.Start == 0 && s.Length == 3);
        }

        [Fact]
        public void ShouldClassifyNumericLiteral()
        {
            var spans = _highlighter.Classify("var x = 1;");

            Assert.Contains(spans, s => s.Kind == ClassificationKind.Number && s.Start == 8 && s.Length == 1);
        }

        [Fact]
        public void ShouldClassifyStringAndComment()
        {
            var spans = _highlighter.Classify("\"hi\" // done");

            Assert.Contains(spans, s => s.Kind == ClassificationKind.String && s.Start == 0 && s.Length == 4);
            Assert.Contains(spans, s => s.Kind == ClassificationKind.Comment && s.Start == 5);
        }

        [Fact]
        public void ShouldClassifyLoadDirective()
        {
            var spans = _highlighter.Classify("#load \"other.csx\"");

            Assert.Contains(spans, s => s.Kind == ClassificationKind.Directive && s.Start == 0);
        }

        [Fact]
        public void ShouldNotTreatBracesInsideStringsAsCode()
        {
            const string text = "var x = \"{\";";
            var mask = SyntaxHighlighter.GetCodeMask(text, _highlighter.Classify(text));

            Assert.False(mask[text.IndexOf('{')]);
            Assert.True(mask[text.IndexOf('=')]);
        }

        [Fact]
        public void ShouldReturnNoSpansForEmptyInput()
        {
            Assert.Empty(_highlighter.Classify(string.Empty));
        }

        [Fact]
        public void ShouldNotOverlapSpans()
        {
            var spans = _highlighter.Classify("if (true) { Console.WriteLine(\"x\"); } // trailing")
                .OrderBy(s => s.Start)
                .ToArray();

            for (var i = 1; i < spans.Length; i++)
            {
                Assert.True(spans[i].Start >= spans[i - 1].End);
            }
        }
    }

    public class ReplHistoryTests
    {
        [Fact]
        public void ShouldIgnoreBlankAndRepeatedEntries()
        {
            var history = new ReplHistory();
            history.Add("1+1");
            history.Add("1+1");
            history.Add("   ");

            Assert.Single(history.Entries);
        }

        [Fact]
        public void ShouldEvictOldestEntryWhenCapacityIsReached()
        {
            var history = new ReplHistory(capacity: 2);
            history.Add("one");
            history.Add("two");
            history.Add("three");

            Assert.Equal(new[] { "two", "three" }, history.Entries);
        }

        [Fact]
        public void ShouldNavigateBackwardsThroughAllEntries()
        {
            var history = new ReplHistory();
            history.Add("one");
            history.Add("two");

            Assert.True(history.TryGetPrevious(string.Empty, 0, out var first));
            Assert.Equal("two", first);

            Assert.True(history.TryGetPrevious(string.Empty, 0, out var second));
            Assert.Equal("one", second);

            Assert.False(history.TryGetPrevious(string.Empty, 0, out _));
        }

        [Fact]
        public void ShouldOnlyReturnEntriesMatchingThePrefix()
        {
            var history = new ReplHistory();
            history.Add("var x = 1;");
            history.Add("Console.WriteLine();");

            Assert.True(history.TryGetPrevious("var", 3, out var entry));
            Assert.Equal("var x = 1;", entry);
        }

        [Fact]
        public void ShouldFindEntryByReverseSearch()
        {
            var history = new ReplHistory();
            history.Add("var x = 1;");
            history.Add("Console.WriteLine();");

            Assert.True(history.TryReverseSearch("writeline", history.Count - 1, out var index, out var entry));
            Assert.Equal(1, index);
            Assert.Equal("Console.WriteLine();", entry);
        }
    }
}
