using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RetroSharp.Rewriters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetroSharp
{
    public class Generator
    {
        public static async Task<Project> MakeRetro(Project prj)
        {
            var compilation = await prj.GetCompilationAsync();

            var changedDocuments = MakeChanges(compilation);

            Project nPrj = prj;

            foreach (var newDoc in changedDocuments)
            {
                var docId = nPrj.GetDocumentId(newDoc.Item1);

                var doc = nPrj.GetDocument(docId);
                nPrj = nPrj.RemoveDocument(docId);

                var nd = nPrj.AddDocument(doc.Name, newDoc.Item2, doc.Folders, doc.FilePath);
                nPrj = nd.Project;
            }

            return nPrj;
        }

        public static IEnumerable<Tuple<SyntaxTree, SyntaxNode>> MakeChanges(Compilation compilation)
        {
            var objectType = compilation.GetTypeByMetadataName("System.Object");

            var changedNodes = new List<Tuple<SyntaxTree, SyntaxNode>>();

            var retroChanges = GetRetroChanges(compilation, objectType);

            foreach (var treeGroup in retroChanges.GroupBy(x => x.Key.SyntaxTree))
            {
                var root = treeGroup.Key.GetRoot();

                var treeAfterReplace = root.ReplaceNodes(treeGroup.Select(x => x.Key), (x, y) => retroChanges[x]);

                // final cleanup: remove generics
                var clean = new RemoveGenericNames().Visit(
                    new RemoveGenericClass().Visit(treeAfterReplace));

                yield return new Tuple<SyntaxTree, SyntaxNode>(treeGroup.Key, clean);
            }

        }

        private static IDictionary<SyntaxNode, SyntaxNode> GetRetroChanges(Compilation compilation, INamedTypeSymbol objectType)
        {
            var syntaxNodes = new Dictionary<SyntaxNode, SyntaxNode>();

            var semanticModels = compilation.SyntaxTrees.Select(x => compilation.GetSemanticModel(x));

            var genRefs = FindGenericClassMembers(semanticModels);

             //CHANGE
             //1. Generic references with cast
             //2. Generic T to Object
             //======

            foreach (var semanticModel in semanticModels)
            {
                var root = semanticModel.SyntaxTree.GetRoot();

                // get cast changes
                var castReferences = new Dictionary<SyntaxNode, SyntaxNode>();
                CastResursiveMethod(root, semanticModel, genRefs, castReferences);
                
                // get generic T to object
                var retroProperties = GenericToObject(root, semanticModel, objectType);

                var allChanges = castReferences.Concat(retroProperties);

                foreach (var ch in allChanges)
                    syntaxNodes.Add(ch.Key, ch.Value);
            }

            return syntaxNodes;
        }

        private static GenRef FindGenericClassMembers(IEnumerable<SemanticModel> semanticModels)
        {
            // FIND
            // 1. Generic properties symbols
            // 2. Generic method symbols
            // =============================

            var genericPropertiesList = new List<IPropertySymbol>();
            var genericMethodsList = new List<IMethodSymbol>();

            foreach (var semanticModel in semanticModels)
            {
                var tree = semanticModel.SyntaxTree;

                // Find generic classes
                var genericClassFinder = new GenericClassFinder(tree, semanticModel);
                var classDeclarations = genericClassFinder.Get().ToArray();

                SyntaxNode newTree = tree.GetRoot();

                // methods symbols
                var methodsToCast = classDeclarations
                    .SelectMany(x => x.Members)
                    .OfType<BaseMethodDeclarationSyntax>()
                    .Select(x => (IMethodSymbol)semanticModel.GetDeclaredSymbol(x));

                genericMethodsList.AddRange(methodsToCast);

                // properties symbols
                var propertiesToCast = classDeclarations
                    .SelectMany(x => x.Members)
                    .OfType<BasePropertyDeclarationSyntax>()
                    .Select(x => (IPropertySymbol)semanticModel.GetDeclaredSymbol(x));

                genericPropertiesList.AddRange(propertiesToCast);
            }

            var genRefs = new GenRef(genericMethodsList, genericPropertiesList);
            return genRefs;
        }

        private static SyntaxNode CastResursiveMethod(SyntaxNode tree, SemanticModel semanticModel, GenRef genRef, Dictionary<SyntaxNode, SyntaxNode> castChanges)
        {
            var change = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (var node in tree.ChildNodes())
            {
                // recurse for changed node
                var casted = CastResursiveMethod(node, semanticModel, genRef, castChanges);

                var ts = GetGenericType(node, genRef, semanticModel);

                if (ts != null)
                {
                    casted = Helpers.CastTo((ExpressionSyntax)casted, ts);

                    if (node.Parent is MemberAccessExpressionSyntax)
                        casted = ((ExpressionSyntax)casted).Parenthesize();

                    castChanges.Add(node, casted);
                }

                if (node != casted)
                    change.Add(node, casted);
            }

            if (change.Any())
                tree = tree.ReplaceNodes(change.Keys, (x, y) => change[x]);

            return tree;
        }

        private static ITypeSymbol GetGenericType(SyntaxNode node, GenRef genRef, SemanticModel semanticModel)
        {
            if (node is InvocationExpressionSyntax)
            {
                ISymbol invokedSymbol = semanticModel.GetSymbolInfo(node).Symbol;

                // if is generic method
                if (genRef.Methods.Contains(invokedSymbol.OriginalDefinition))
                {
                    return ((IMethodSymbol)invokedSymbol).ReturnType;
                }
            }
            else if ((node is MemberAccessExpressionSyntax) && !(node.Parent is AssignmentExpressionSyntax))
            {
                ISymbol invokedSymbol = semanticModel.GetSymbolInfo(node).Symbol;

                // if is generic property
                if (genRef.Properties.Contains(invokedSymbol.OriginalDefinition))
                {
                    return ((IPropertySymbol)invokedSymbol).Type;
                }
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GenericToObject(SyntaxNode tree, SemanticModel semanticModel, INamedTypeSymbol symbol)
        {
            foreach (var node in tree.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var info = semanticModel.GetDeclaredSymbol(node);
                
                if (info.IsGenericType)
                {
                    var typeParameters = new HashSet<ITypeParameterSymbol>(info.TypeParameters);

                    foreach (var changedPart in GetCleanedGenericClass(node, semanticModel, typeParameters, symbol))
                        yield return changedPart;
                }
            }
        }

        private static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetCleanedGenericClass(SyntaxNode tree, SemanticModel semanticModel, ISet<ITypeParameterSymbol> typeParameters, INamedTypeSymbol symbol)
        {
            foreach (var node in tree.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var s = semanticModel.GetSymbolInfo(node);

                if (typeParameters.Contains(s.Symbol))
                {
                    var typeSynax = SyntaxFactory.IdentifierName(symbol.ToDisplayString())
                                .WithLeadingTrivia(node.GetLeadingTrivia())
                                .WithTrailingTrivia(node.GetTrailingTrivia());

                    yield return new KeyValuePair<SyntaxNode, SyntaxNode>(node, typeSynax);
                }
            }
        }
    }

    class GenRef
    {
        public readonly HashSet<ISymbol> Methods = new HashSet<ISymbol>();
        public readonly HashSet<ISymbol> Properties = new HashSet<ISymbol>();

        public GenRef(IEnumerable<ISymbol> methods, IEnumerable<ISymbol> properties)
        {
            this.Methods = new HashSet<ISymbol>(methods);
            this.Properties = new HashSet<ISymbol>(properties);
        }
    }
}
