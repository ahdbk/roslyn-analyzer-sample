using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace EmtyArray
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmtyArrayAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EmtyArray";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // On enregistre une action qui va se déclancher pour chaque operation de "return" détectée
            context.RegisterOperationAction(AnalyzeReturnStatement, OperationKind.Return);
        }


        private void AnalyzeReturnStatement(OperationAnalysisContext context)
        {
            // On recupere notre operation de retour depuis le context
            var operation = context.Operation as IReturnOperation;

            // on récupere l'arbre syntaxique et le modele sementique de cette operation
            var syntaxTree = context.Operation.Syntax;
            var sementicModel = context.Operation.SemanticModel;

            // on parcoure touts les noeuds de notre operation pour voir
            // si il y a une operation de creation de tableau dans notre operation de retour
            var arrayCreationExpression = syntaxTree.ChildNodes()
                .SingleOrDefault(el => el.Kind() == SyntaxKind.ArrayCreationExpression);

            // si on arrive a trouver une operation de creation de tableau dans notre "return"
            if (arrayCreationExpression != null)
            {
                // on demande a notre sementicModel plus d'information sur notre operation
                // comme ça on peut avoir la dimension du tableau
                var arrayCreationOp = (IArrayCreationOperation)sementicModel.GetOperation(arrayCreationExpression);

                if (IsArrayEmpty(arrayCreationOp))
                {
                    // si notre tableau est vide on crée un diagnostique et le signale
                    var diagnostic = Diagnostic.Create(Rule, syntaxTree.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }

            bool IsArrayEmpty(IArrayCreationOperation arrayCreationOp)
            {
                return arrayCreationOp.DimensionSizes.First().ConstantValue.HasValue
                    && (int)arrayCreationOp.DimensionSizes.First().ConstantValue.Value == 0;
            }

        }


    }
}
