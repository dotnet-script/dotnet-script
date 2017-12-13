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
            var currentNode = base.VisitLoadDirectiveTrivia(node);
            var skippedTrivia = currentNode.DescendantTrivia().Where(x => x.RawKind == (int)SyntaxKind.SkippedTokensTrivia).FirstOrDefault();
            return currentNode.ReplaceTrivia(skippedTrivia, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed, skippedTrivia));
        }

        public override SyntaxNode VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node)
        {
            var currentNode = base.VisitReferenceDirectiveTrivia(node);
            var skippedTrivia = currentNode.DescendantTrivia().Where(x => x.RawKind == (int)SyntaxKind.SkippedTokensTrivia).FirstOrDefault();
            return currentNode.ReplaceTrivia(skippedTrivia, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed, skippedTrivia));
        }
    }
}