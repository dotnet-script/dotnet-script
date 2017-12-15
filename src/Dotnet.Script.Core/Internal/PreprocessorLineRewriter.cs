using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnet.Script
{
    internal class PreprocessorLineRewriter : CSharpSyntaxRewriter
    {
        public PreprocessorLineRewriter()
            : base(visitIntoStructuredTrivia: true)
        {
        }

        public override SyntaxNode VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node)
        {
            return HandleSkippedTrivia(base.VisitLoadDirectiveTrivia(node));
        }

        public override SyntaxNode VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node)
        {
            return HandleSkippedTrivia(base.VisitReferenceDirectiveTrivia(node));
        }

        private SyntaxNode HandleSkippedTrivia(SyntaxNode node)
        {
            var skippedTrivia = node.DescendantTrivia().Where(x => x.RawKind == (int)SyntaxKind.SkippedTokensTrivia).FirstOrDefault();
            if (skippedTrivia != null)
            {
                var firstToken = skippedTrivia.GetStructure().ChildTokens().FirstOrDefault();
                if (firstToken != null && firstToken.Kind() == SyntaxKind.BadToken && firstToken.ToFullString().Trim() == ";")
                {
                    node = node.ReplaceToken(firstToken, SyntaxFactory.Token(SyntaxKind.None));
                    skippedTrivia = node.DescendantTrivia().Where(x => x.RawKind == (int)SyntaxKind.SkippedTokensTrivia).FirstOrDefault();
                }

                node = node.ReplaceTrivia(skippedTrivia, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed, skippedTrivia));

                return node;
            }

            return node;
        }
    }
}