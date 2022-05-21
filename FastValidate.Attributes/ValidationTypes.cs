using System;

namespace FastValidate.Attributes
{
    [Flags]
    internal enum ValidationType
    {
        Numeric = 8,
        Range = 9,
        Bound = 10
    }
    internal interface IValidation {}
    internal interface INumericValidation : IValidation { }
}