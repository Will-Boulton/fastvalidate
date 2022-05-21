using FastValidate.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.SourceGen.Validations.Numerics;

public class RangeValidation : IMemberValidation, INumericValidation
{
    internal RangeValidation(MemberDeclarationSyntax member, object value1, object value2)
    {
        Member = member;
        Value1 = value1;
        Value2 = value2;
    }
    public object Value1 { get; }
    public object Value2 { get; }
    public MemberDeclarationSyntax Member { get; }
    
}