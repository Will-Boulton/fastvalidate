using FastValidate.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.SourceGen.Validations.Numerics;

public class BoundValidation : IMemberValidation, INumericValidation
{
    private BoundValidation(MemberDeclarationSyntax member, object value, bool isGreaterThanCheck)
    {
        Member = member;
        Value = value;
        IsGreaterThanCheck = isGreaterThanCheck;
    }

    internal static BoundValidation GreaterThan(MemberDeclarationSyntax member, object value)
        => new BoundValidation(member, value, true);

    internal static BoundValidation LessThan(MemberDeclarationSyntax member, object value)
        => new BoundValidation(member, value, false);
    
    public bool IsGreaterThanCheck { get; }
    public object Value { get; }
    public MemberDeclarationSyntax Member { get; }
    
}