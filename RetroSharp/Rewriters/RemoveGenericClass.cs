using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RetroSharp.Rewriters
{
    public class RemoveGenericClass : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var typeList = node.ChildNodes().OfType<TypeParameterListSyntax>();

            if (typeList.Any())
                return node.RemoveNodes(typeList, SyntaxRemoveOptions.KeepEndOfLine);

            return node;
        }
    }
}
