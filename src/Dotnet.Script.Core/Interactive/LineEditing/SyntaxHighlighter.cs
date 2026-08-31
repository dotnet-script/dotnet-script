using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Classifies REPL input using the Roslyn lexer. Only <c>Microsoft.CodeAnalysis.CSharp</c>
    /// is required, so no additional (heavy) workspace packages are pulled in.
    /// </summary>
    public sealed class SyntaxHighlighter : ISyntaxClassifier
    {
        private static readonly CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.Preview, kind: SourceCodeKind.Script);

        private static readonly ClassifiedSpan[] NoSpans = new ClassifiedSpan[0];

        public IReadOnlyList<ClassifiedSpan> Classify(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return NoSpans;
            }

            var spans = new List<ClassifiedSpan>();
            try
            {
                foreach (var token in SyntaxFactory.ParseTokens(text, 0, 0, ParseOptions))
                {
                    AddTrivia(spans, token.LeadingTrivia);

                    if (token.Span.Length > 0)
                    {
                        Add(spans, token.SpanStart, token.Span.Length, GetKind(token));
                    }

                    AddTrivia(spans, token.TrailingTrivia);

                    if (token.IsKind(SyntaxKind.EndOfFileToken))
                    {
                        break;
                    }
                }
            }
            catch (System.Exception)
            {
                // Classification is cosmetic - never let it break the editor.
                return NoSpans;
            }

            return spans;
        }

        /// <summary>
        /// Returns a mask where positions that are part of actual code (i.e. not inside
        /// a string, comment or directive) are <c>true</c>.
        /// </summary>
        public static bool[] GetCodeMask(string text, IReadOnlyList<ClassifiedSpan> spans)
        {
            var mask = new bool[text?.Length ?? 0];
            for (var i = 0; i < mask.Length; i++)
            {
                mask[i] = true;
            }

            if (spans == null)
            {
                return mask;
            }

            foreach (var span in spans)
            {
                if (span.Kind != ClassificationKind.String &&
                    span.Kind != ClassificationKind.Comment &&
                    span.Kind != ClassificationKind.Directive)
                {
                    continue;
                }

                for (var i = span.Start; i < span.End && i < mask.Length; i++)
                {
                    mask[i] = false;
                }
            }

            return mask;
        }

        private static void AddTrivia(List<ClassifiedSpan> spans, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.Span.Length == 0)
                {
                    continue;
                }

                if (trivia.IsDirective)
                {
                    Add(spans, trivia.SpanStart, trivia.Span.Length, ClassificationKind.Directive);
                    continue;
                }

                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.DisabledTextTrivia:
                        Add(spans, trivia.SpanStart, trivia.Span.Length, ClassificationKind.Comment);
                        break;
                }
            }
        }

        private static void Add(List<ClassifiedSpan> spans, int start, int length, ClassificationKind kind)
        {
            if (kind == ClassificationKind.Default || length <= 0)
            {
                return;
            }

            spans.Add(new ClassifiedSpan(start, length, kind));
        }

        private static ClassificationKind GetKind(SyntaxToken token)
        {
            var kind = token.Kind();

            switch (kind)
            {
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                case SyntaxKind.Utf8StringLiteralToken:
                case SyntaxKind.InterpolatedStringToken:
                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.InterpolatedStringStartToken:
                case SyntaxKind.InterpolatedStringEndToken:
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                    return ClassificationKind.String;
                case SyntaxKind.NumericLiteralToken:
                    return ClassificationKind.Number;
                case SyntaxKind.IdentifierToken:
                    // The lexer has no semantic context, so a variable named "value" or "record"
                    // is highlighted as a keyword. Acceptable trade-off for a lexer-only classifier.
                    return SyntaxFacts.GetContextualKeywordKind(token.ValueText) != SyntaxKind.None
                        ? ClassificationKind.Keyword
                        : ClassificationKind.Identifier;
            }

            if (SyntaxFacts.IsKeywordKind(kind))
            {
                return ClassificationKind.Keyword;
            }

            if (SyntaxFacts.IsPunctuation(kind))
            {
                return ClassificationKind.Punctuation;
            }

            return ClassificationKind.Default;
        }
    }
}
