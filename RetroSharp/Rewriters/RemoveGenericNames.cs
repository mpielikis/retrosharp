using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RetroSharp.Rewriters
{
    public class RemoveGenericNames : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var typeArgument = node.ChildNodes().OfType<TypeArgumentListSyntax>();

            if (typeArgument.Any())
                return SyntaxFactory.IdentifierName(node.Identifier);
            
            return node;
        }
    }
}
