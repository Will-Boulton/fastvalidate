using System;

namespace FastValidate.Attributes
{
    
    /// <summary>
    ///     Mark a type to generate validation for it
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class ValidateAttribute : Attribute { }
}