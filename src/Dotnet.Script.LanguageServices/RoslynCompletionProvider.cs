using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dotnet.Script.Core.Interactive.LineEditing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace Dotnet.Script.LanguageServices
{
    public sealed class RoslynCompletionProvider : ICompletionProvider
    {
        // Tab blocks the editor loop, so this is the longest the REPL may appear frozen. Warming the
        // workspace up front is what keeps it from ever being reached in practice.
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private readonly ReplWorkspace _workspace;

        public RoslynCompletionProvider(ReplWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        /// <summary>
        /// Returns <c>null</c> when Roslyn could not answer at all, which lets the caller fall back, and an
        /// empty result when it answered that there is nothing to offer here.
        /// </summary>
        public CompletionResult GetCompletions(string text, int caret)
        {
            if (text == null)
            {
                return null;
            }

            caret = Math.Max(0, Math.Min(caret, text.Length));

            var document = _workspace.GetDocument(text);
            if (document == null)
            {
                return null;
            }

            var service = CompletionService.GetService(document);
            if (service == null)
            {
                return null;
            }

            var completions = BoundedOperation.Run(
                token => service.GetCompletionsAsync(document, caret, CompletionTrigger.Invoke, cancellationToken: token),
                Timeout,
                null);

            if (completions == null)
            {
                return null;
            }

            var start = Math.Max(0, Math.Min(completions.Span.Start, caret));
            var prefix = text.Substring(start, caret - start);

            return new CompletionResult(start, prefix.Length, Filter(service, document, completions, prefix));
        }

        private static IReadOnlyList<string> Filter(CompletionService service, Document document, CompletionList completions, string prefix)
        {
            var candidates = completions.ItemsList
                // A non empty inline description marks a type that still needs its namespace imported;
                // inserting the short name alone would not compile.
                .Where(item => string.IsNullOrEmpty(item.InlineDescription))
                .ToImmutableArray();

            // Roslyn's own filtering gives the same camel case matching an IDE has, so "WL" finds WriteLine.
            var matches = prefix.Length == 0 ? candidates : service.FilterItems(document, candidates, prefix);

            return matches
                .OrderBy(item => item.SortText, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.DisplayText)
                .Where(display => !string.IsNullOrEmpty(display))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
