using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace RetroSharp
{
    public class GenericClassFinder
    {
        private readonly SyntaxTree tree;
        private readonly SemanticModel semanticModel;

        public GenericClassFinder(SyntaxTree tree, SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
            this.tree = tree;
        }

        public IEnumerable<ClassDeclarationSyntax> Get()
        {
            foreach (var clsDecl in tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>())
            {
                var info = semanticModel.GetDeclaredSymbol(clsDecl);

                if (info.IsGenericType)
                    yield return clsDecl;
            }
        }
    }
}
