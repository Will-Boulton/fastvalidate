using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.Validations;

internal interface IMemberValidation
{
    public string MemberName { get; }
}