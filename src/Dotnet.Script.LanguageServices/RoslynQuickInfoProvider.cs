using System;
using System.Collections.Generic;
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
    /// argument list is answered by listing the overloads of the member being invoked.
    /// </summary>
    public sealed class RoslynQuickInfoProvider : IQuickInfoProvider
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly string[] Nothing = new string[0];

        private static readonly SymbolDisplayFormat SignatureFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                           SymbolDisplayMemberOptions.IncludeType |
                           SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeName |
                              SymbolDisplayParameterOptions.IncludeParamsRefOut |
                              SymbolDisplayParameterOptions.IncludeDefaultValue |
                              SymbolDisplayParameterOptions.IncludeExtensionThis,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                  SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private readonly ReplWorkspace _workspace;

        public RoslynQuickInfoProvider(ReplWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public IReadOnlyList<string> GetQuickInfo(string text, int caret)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Nothing;
            }

            caret = Math.Max(0, Math.Min(caret, text.Length));

            var document = _workspace.GetDocument(text);
            if (document == null)
            {
                return Nothing;
            }

            return BoundedOperation.Run(token => DescribeAsync(document, caret, token), Timeout, Nothing);
        }

        private static async Task<IReadOnlyList<string>> DescribeAsync(Document document, int caret, CancellationToken cancellationToken)
        {
            var overloads = await GetOverloadsAsync(document, caret, cancellationToken).ConfigureAwait(false);

            return overloads.Count > 0
                ? overloads
                : await DescribeSymbolAsync(document, Math.Max(0, caret - 1), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Inside an argument list the caret sits on an argument, which is rarely what the user wants to
        /// read about - walk out to the member being invoked and list its whole method group.
        /// </summary>
        private static async Task<IReadOnlyList<string>> GetOverloadsAsync(Document document, int caret, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return Nothing;
            }

            var node = root.FindToken(Math.Max(0, caret - 1)).Parent
                ?.AncestorsAndSelf()
                .FirstOrDefault(candidate =>
                    candidate is InvocationExpressionSyntax invocation && invocation.ArgumentList.SpanStart < caret ||
                    candidate is ObjectCreationExpressionSyntax creation && creation.ArgumentList?.SpanStart < caret);

            if (node == null)
            {
                return Nothing;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var group = node is InvocationExpressionSyntax invoked
                ? semanticModel.GetMemberGroup(invoked.Expression, cancellationToken)
                : semanticModel.GetMemberGroup(node, cancellationToken);

            return group
                .OfType<IMethodSymbol>()
                .Select(method => method.ToDisplayString(SignatureFormat))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(signature => signature.Count(character => character == ','))
                .ThenBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
        }

        private static async Task<IReadOnlyList<string>> DescribeSymbolAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var service = QuickInfoService.GetService(document);
            if (service == null)
            {
                return Nothing;
            }

            var item = await service.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);

            var description = item?.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.Description);
            if (description == null)
            {
                return Nothing;
            }

            var line = Whitespace.Replace(description.Text, " ").Trim();

            return line.Length == 0 ? Nothing : new[] { line };
        }
    }
}
