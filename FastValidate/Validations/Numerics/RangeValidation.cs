using FastValidate.Attributes;

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

    public uint FuzzyOrdinal => 0;
    public string MemberName { get; }
    public string SourceString => $"({MemberName} is > {Value1} and < {Value2})";

}