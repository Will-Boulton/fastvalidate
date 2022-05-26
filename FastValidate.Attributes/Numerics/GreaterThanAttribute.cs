using System;

namespace FastValidate.Attributes
{
    public sealed partial class Validate
    {
        public sealed class GreaterThanAttribute : Attribute, INumericValidation
        {
            public GreaterThanAttribute(sbyte value){}
            public GreaterThanAttribute(byte value){}
            public GreaterThanAttribute(short value){}
            public GreaterThanAttribute(ushort value){}
            public GreaterThanAttribute(int value){}
            public GreaterThanAttribute(uint value){}
            public GreaterThanAttribute(long value){}
            public GreaterThanAttribute(ulong value){}
            public GreaterThanAttribute(float value){}
            public GreaterThanAttribute(double value){}
            public GreaterThanAttribute(decimal value){}
        }        
    }
}