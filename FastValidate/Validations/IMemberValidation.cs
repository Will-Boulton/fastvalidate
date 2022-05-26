using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.Validations;

internal interface IMemberValidation
{
    public uint FuzzyOrdinal { get; }
    public string MemberName { get; }
    
    public string SourceString { get; }
}
