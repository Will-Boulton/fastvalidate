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
    public bool Inclusive { get; internal set; }
    public bool IsGreaterThanCheck { get; internal set; }
    public object Value { get; }

    public uint FuzzyOrdinal => 0;
    public string MemberName { get; }

    private string Operator => string.Format("{0}{1}",
        IsGreaterThanCheck switch { true => ">", false => "<" },
        Inclusive switch { true => "=", false => "" });
    
    public string SourceString => $"({MemberName} {Operator} {Value})";

}