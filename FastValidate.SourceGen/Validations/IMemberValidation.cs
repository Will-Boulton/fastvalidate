using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.SourceGen.Validations;

internal interface IMemberValidation
{
    public MemberDeclarationSyntax Member { get; }
}