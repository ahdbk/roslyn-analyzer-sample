using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmtyArray
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmtyArrayCodeFixProvider)), Shared]
    public class EmtyArrayCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EmtyArrayAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: async c => await ArrayToEmptyAsync(context.Document, diagnostic, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> ArrayToEmptyAsync(Document document, Diagnostic diagnostic, CancellationToken c)
        {
            var syntaxTreeRoot = await document.GetSyntaxRootAsync();
            var sementicModel = await document.GetSemanticModelAsync();
            var generator = SyntaxGenerator.GetGenerator(document);

            // Get current node
            var currentNode = syntaxTreeRoot.FindNode(diagnostic.Location.SourceSpan);

            // Build new node
            // get array element types
            var arrayType = sementicModel.Compilation.GetTypeByMetadataName("System.Array");
            var arraySyntaxExpression = generator.TypeExpression(arrayType);
            var childs = currentNode.ChildNodes().ToList();
            // build generic name Empty
            // add Array type to generic (member statment)
            var arrayCreationExpression = (ArrayCreationExpressionSyntax)currentNode.ChildNodes()
                .Single(el => el.Kind() == SyntaxKind.ArrayCreationExpression);

            var elementType = arrayCreationExpression.Type.ElementType;
            var genericExpression = generator.GenericName("Empty", elementType);

            var memberExpression = generator.MemberAccessExpression(arraySyntaxExpression, genericExpression);

            var invocationExpression = generator.InvocationExpression(memberExpression);

            var returnExpression = generator.ReturnStatement(invocationExpression);
            // add return operation
            // replace node
            // return new syntaxTree
            var newSyntaxTree = syntaxTreeRoot.ReplaceNode(currentNode, returnExpression);
            var newDoc = document.WithSyntaxRoot(newSyntaxTree);

            return newDoc.Project.Solution;

        }

    }
}
