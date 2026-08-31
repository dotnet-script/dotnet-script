using System;
using System.Collections.Generic;
using System.Linq;
using Dotnet.Script.Core.Interactive.LineEditing;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using RoslynClassifiedSpan = Microsoft.CodeAnalysis.Classification.ClassifiedSpan;
using ReplClassifiedSpan = Dotnet.Script.Core.Interactive.LineEditing.ClassifiedSpan;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// Semantic classification. Falls back to the lexer based classifier whenever the semantic model is
    /// not ready in time, so typing never stalls waiting for a compilation.
    /// </summary>
    public sealed class RoslynClassifier : ISyntaxClassifier
    {
        // Measured steady state on a warm workspace is 5-21 ms per key stroke, with the one-off cost of
        // starting the classifier paid during warm up. The lexer fallback is always acceptable, so the
        // budget is set to cover the normal case and give up well before a stutter becomes visible.
        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(50);

        private readonly ReplWorkspace _workspace;
        private readonly ISyntaxClassifier _fallback;

        public RoslynClassifier(ReplWorkspace workspace, ISyntaxClassifier fallback)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public IReadOnlyList<ReplClassifiedSpan> Classify(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<ReplClassifiedSpan>();
            }

            var document = _workspace.GetDocument(text);
            if (document == null)
            {
                return _fallback.Classify(text);
            }

            var classified = BoundedOperation.Run(
                token => Classifier.GetClassifiedSpansAsync(document, new TextSpan(0, text.Length), token),
                Timeout,
                null);

            return classified == null ? _fallback.Classify(text) : Flatten(classified, text.Length);
        }

        /// <summary>
        /// Roslyn emits additive and overlapping classifications; the renderer needs a single ordered,
        /// non overlapping run per position.
        /// </summary>
        private static IReadOnlyList<ReplClassifiedSpan> Flatten(IEnumerable<RoslynClassifiedSpan> classified, int length)
        {
            var kinds = new ClassificationKind[length];

            foreach (var span in classified.OrderBy(s => s.TextSpan.Start))
            {
                var kind = Map(span.ClassificationType);
                if (kind == ClassificationKind.Default)
                {
                    continue;
                }

                for (var i = span.TextSpan.Start; i < span.TextSpan.End && i < length; i++)
                {
                    if (kinds[i] == ClassificationKind.Default)
                    {
                        kinds[i] = kind;
                    }
                }
            }

            var spans = new List<ReplClassifiedSpan>();
            var start = 0;
            while (start < length)
            {
                var kind = kinds[start];
                var end = start;
                while (end < length && kinds[end] == kind)
                {
                    end++;
                }

                if (kind != ClassificationKind.Default)
                {
                    spans.Add(new ReplClassifiedSpan(start, end - start, kind));
                }

                start = end;
            }

            return spans;
        }

        private static ClassificationKind Map(string classificationType)
        {
            switch (classificationType)
            {
                case ClassificationTypeNames.Keyword:
                case ClassificationTypeNames.ControlKeyword:
                case ClassificationTypeNames.PreprocessorKeyword:
                    return ClassificationKind.Keyword;

                case ClassificationTypeNames.StringLiteral:
                case ClassificationTypeNames.VerbatimStringLiteral:
                case ClassificationTypeNames.StringEscapeCharacter:
                    return ClassificationKind.String;

                case ClassificationTypeNames.NumericLiteral:
                    return ClassificationKind.Number;

                case ClassificationTypeNames.Comment:
                case ClassificationTypeNames.XmlDocCommentText:
                case ClassificationTypeNames.XmlDocCommentDelimiter:
                case ClassificationTypeNames.XmlDocCommentName:
                    return ClassificationKind.Comment;

                case ClassificationTypeNames.PreprocessorText:
                case ClassificationTypeNames.ExcludedCode:
                    return ClassificationKind.Directive;

                case ClassificationTypeNames.Punctuation:
                case ClassificationTypeNames.Operator:
                case ClassificationTypeNames.OperatorOverloaded:
                    return ClassificationKind.Punctuation;

                case ClassificationTypeNames.ClassName:
                case ClassificationTypeNames.StructName:
                case ClassificationTypeNames.InterfaceName:
                case ClassificationTypeNames.EnumName:
                case ClassificationTypeNames.DelegateName:
                case ClassificationTypeNames.RecordClassName:
                case ClassificationTypeNames.RecordStructName:
                case ClassificationTypeNames.TypeParameterName:
                case ClassificationTypeNames.NamespaceName:
                    return ClassificationKind.Type;

                case ClassificationTypeNames.MethodName:
                case ClassificationTypeNames.ExtensionMethodName:
                    return ClassificationKind.Method;

                default:
                    return ClassificationKind.Default;
            }
        }
    }
}
