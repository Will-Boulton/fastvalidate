using FastValidate.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastValidate.Validations.Numerics;

public class BoundValidation : IMemberValidation, INumericValidation
{
    internal BoundValidation(string memberName, object value)
    {
        MemberName = memberName;
        Value = value;
    }

    public bool IsGreaterThanCheck { get; internal set; }
    public object Value { get; }
    public string MemberName { get; }
    
}