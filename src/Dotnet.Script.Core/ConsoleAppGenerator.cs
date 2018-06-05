using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Dotnet.Script.Core
{
    public static class ConsoleAppGenerator
    {
        public static CompilationUnitSyntax Generate(string programName)
        {
            var compilationUnit =
CompilationUnit()
.WithMembers(
    SingletonList<MemberDeclarationSyntax>(
        ClassDeclaration("Program")
        .WithMembers(
            SingletonList<MemberDeclarationSyntax>(
                MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier("Main"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.StaticKeyword)))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("System"),
                                            IdentifierName("Console")),
                                        IdentifierName("WriteLine")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal("ohhh boy..."))))))))))))))
.NormalizeWhitespace();

            return compilationUnit;
        }
        //        public static CompilationUnitSyntax Generate(string programName)
        //        {
        //            var compilationUnit =
        //CompilationUnit()
        //.WithMembers(
        //    SingletonList<MemberDeclarationSyntax>(
        //        ClassDeclaration("Program")
        //        .WithMembers(
        //            SingletonList<MemberDeclarationSyntax>(
        //                MethodDeclaration(
        //                    PredefinedType(
        //                        Token(SyntaxKind.VoidKeyword)),
        //                    Identifier("Main"))
        //                .WithModifiers(
        //                    TokenList(
        //                        Token(SyntaxKind.StaticKeyword)))
        //                .WithBody(
        //                    Block(
        //                        SingletonList<StatementSyntax>(
        //                            ExpressionStatement(
        //                                InvocationExpression(
        //                                    MemberAccessExpression(
        //                                        SyntaxKind.SimpleMemberAccessExpression,
        //                                        MemberAccessExpression(
        //                                            SyntaxKind.SimpleMemberAccessExpression,
        //                                            IdentifierName("System"),
        //                                            IdentifierName("Console")),
        //                                        IdentifierName("WriteLine")))
        //                                .WithArgumentList(
        //                                    ArgumentList(
        //                                        SingletonSeparatedList<ArgumentSyntax>(
        //                                            Argument(
        //                                                LiteralExpression(
        //                                                    SyntaxKind.StringLiteralExpression,
        //                                                    Literal("ohhh boy..."))))))))))))))
        //.NormalizeWhitespace();

        //            return compilationUnit;
        //        }
        //        public static CompilationUnitSyntax Generate(string programName)
        //        {
        //            var compilationUnit =
        //CompilationUnit()
        //.WithMembers(
        //    SingletonList<MemberDeclarationSyntax>(
        //        NamespaceDeclaration(
        //            IdentifierName("ConsoleApp"))
        //        .WithMembers(
        //            SingletonList<MemberDeclarationSyntax>(
        //                ClassDeclaration("Program")
        //                .WithMembers(
        //                    SingletonList<MemberDeclarationSyntax>(
        //                        MethodDeclaration(
        //                            PredefinedType(
        //                                Token(SyntaxKind.VoidKeyword)),
        //                            Identifier("Main"))
        //                        .WithModifiers(
        //                            TokenList(
        //                                Token(SyntaxKind.StaticKeyword)))
        //                        .WithBody(
        //                            Block(
        //                                LocalDeclarationStatement(
        //                                    VariableDeclaration(
        //                                        IdentifierName("var"))
        //                                    .WithVariables(
        //                                        SingletonSeparatedList<VariableDeclaratorSyntax>(
        //                                            VariableDeclarator(
        //                                                Identifier("globalContext"))
        //                                            .WithInitializer(
        //                                                EqualsValueClause(
        //                                                    ObjectCreationExpression(
        //                                                        IdentifierName(programName))
        //                                                    .WithArgumentList(
        //                                                        ArgumentList(
        //                                                            SingletonSeparatedList<ArgumentSyntax>(
        //                                                                Argument(
        //                                                                    ArrayCreationExpression(
        //                                                                        ArrayType(
        //                                                                            PredefinedType(
        //                                                                                Token(SyntaxKind.ObjectKeyword)))
        //                                                                        .WithRankSpecifiers(
        //                                                                            SingletonList<ArrayRankSpecifierSyntax>(
        //                                                                                ArrayRankSpecifier(
        //                                                                                    SingletonSeparatedList<ExpressionSyntax>(
        //                                                                                        OmittedArraySizeExpression())))))
        //                                                                    .WithInitializer(
        //                                                                        InitializerExpression(
        //                                                                            SyntaxKind.ArrayInitializerExpression,
        //                                                                            SeparatedList<ExpressionSyntax>(
        //                                                                                new SyntaxNodeOrToken[]{
        //                                                                                    LiteralExpression(
        //                                                                                        SyntaxKind.NullLiteralExpression),
        //                                                                                    Token(SyntaxKind.CommaToken),
        //                                                                                    LiteralExpression(
        //                                                                                        SyntaxKind.NullLiteralExpression)})))))))))))),
        //                                LocalDeclarationStatement(
        //                                    VariableDeclaration(
        //                                        IdentifierName("var"))
        //                                    .WithVariables(
        //                                        SingletonSeparatedList<VariableDeclaratorSyntax>(
        //                                            VariableDeclarator(
        //                                                Identifier("methodInfo"))
        //                                            .WithInitializer(
        //                                                EqualsValueClause(
        //                                                    InvocationExpression(
        //                                                        MemberAccessExpression(
        //                                                            SyntaxKind.SimpleMemberAccessExpression,
        //                                                            TypeOfExpression(
        //                                                                IdentifierName(programName)),
        //                                                            IdentifierName("GetMethod")))
        //                                                    .WithArgumentList(
        //                                                        ArgumentList(
        //                                                            SingletonSeparatedList<ArgumentSyntax>(
        //                                                                Argument(
        //                                                                    LiteralExpression(
        //                                                                        SyntaxKind.StringLiteralExpression,
        //                                                                        Literal("<Initialize>"))))))))))),
        //                                IfStatement(
        //                                    BinaryExpression(
        //                                        SyntaxKind.EqualsExpression,
        //                                        IdentifierName("methodInfo"),
        //                                        LiteralExpression(
        //                                            SyntaxKind.NullLiteralExpression)),
        //                                    ThrowStatement(
        //                                        ObjectCreationExpression(
        //                                            QualifiedName(
        //                                                IdentifierName("System"),
        //                                                IdentifierName("Exception")))
        //                                        .WithArgumentList(
        //                                            ArgumentList(
        //                                                SingletonSeparatedList<ArgumentSyntax>(
        //                                                    Argument(
        //                                                        LiteralExpression(
        //                                                            SyntaxKind.StringLiteralExpression,
        //                                                            Literal("couldn't find method")))))))),
        //                                ExpressionStatement(
        //                                    InvocationExpression(
        //                                        MemberAccessExpression(
        //                                            SyntaxKind.SimpleMemberAccessExpression,
        //                                            IdentifierName("methodInfo"),
        //                                            IdentifierName("Invoke")))
        //                                    .WithArgumentList(
        //                                        ArgumentList(
        //                                            SeparatedList<ArgumentSyntax>(
        //                                                new SyntaxNodeOrToken[]{
        //                                                    Argument(
        //                                                        IdentifierName("globalContext")),
        //                                                    Token(SyntaxKind.CommaToken),
        //                                                    Argument(
        //                                                        LiteralExpression(
        //                                                            SyntaxKind.NullLiteralExpression))}))))))))))))
        //.NormalizeWhitespace();

        //            return compilationUnit;
        //        }
    }
}
