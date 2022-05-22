using FastValidate.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.Validations.Numerics;

public class RangeValidation : IMemberValidation, INumericValidation
{
    internal RangeValidation(string memberName, object value1, object value2)
    {
        MemberName = memberName;
        Value1 = value1;
        Value2 = value2;
    }
    public object Value1 { get; }
    public object Value2 { get; }
    public string MemberName { get; }
    
}