using System.Collections.Generic;
using System.Linq;
using FastValidate.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate;

internal class ValidateTypeReceiver : ISyntaxContextReceiver
{
    private readonly List<GeneratorSyntaxContext> _candidateTypes = new ();

    public IReadOnlyList<GeneratorSyntaxContext> CandidateTypes => _candidateTypes;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is TypeDeclarationSyntax tds)
        {
            var ts = context.SemanticModel.GetDeclaredSymbol(tds) as ITypeSymbol;

            if (ts?.GetAttributes().Any(a => a.AttributeClass?.MetadataName == typeof(GenerateValidateMethodAttribute).Name) ?? false)
            {
                _candidateTypes.Add(context);
                return;
            }
            if (ts?.Interfaces.Any(i => i.MetadataName == typeof(IFastValidatable).Name) ?? false)
            {
                _candidateTypes.Add(context);
            }
        }
    }
}