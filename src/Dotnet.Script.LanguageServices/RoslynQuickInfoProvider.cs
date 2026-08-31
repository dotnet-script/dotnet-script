using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Script.Core.Interactive.LineEditing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// Describes the symbol at the caret. Roslyn exposes no public signature help service, so an open
    /// argument list is answered by describing the invoked member instead.
    /// </summary>
    public sealed class RoslynQuickInfoProvider : IQuickInfoProvider
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        private readonly ReplWorkspace _workspace;

        public RoslynQuickInfoProvider(ReplWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public string GetQuickInfo(string text, int caret)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            caret = Math.Max(0, Math.Min(caret, text.Length));

            var document = _workspace.GetDocument(text);
            if (document == null)
            {
                return null;
            }

            return BoundedOperation.Run(token => DescribeAsync(document, caret, token), Timeout, null);
        }

        private static async Task<string> DescribeAsync(Document document, int caret, CancellationToken cancellationToken)
        {
            var service = QuickInfoService.GetService(document);
            if (service == null)
            {
                return null;
            }

            var position = await GetPositionAsync(document, caret, cancellationToken).ConfigureAwait(false);
            var item = await service.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);

            var description = item?.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.Description);
            if (description == null)
            {
                return null;
            }

            var line = Whitespace.Replace(description.Text, " ").Trim();

            return line.Length == 0 ? null : line;
        }

        /// <summary>
        /// Inside an argument list the caret sits on an argument, which is rarely what the user wants to
        /// read about - walk out to the member being invoked.
        /// </summary>
        private static async Task<int> GetPositionAsync(Document document, int caret, CancellationToken cancellationToken)
        {
            var fallback = Math.Max(0, caret - 1);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return fallback;
            }

            var invocation = root.FindToken(fallback).Parent
                ?.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(candidate => candidate.ArgumentList.SpanStart < caret);

            if (invocation == null)
            {
                return fallback;
            }

            return invocation.Expression is MemberAccessExpressionSyntax member
                ? member.Name.SpanStart
                : invocation.Expression.SpanStart;
        }
    }
}
