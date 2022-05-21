using System;

namespace FastValidate.Attributes
{
    public sealed class LessThanAttribute : Attribute, INumericValidation
    {
        public LessThanAttribute(sbyte value){}
        public LessThanAttribute(byte value){}
        public LessThanAttribute(short value){}
        public LessThanAttribute(ushort value){}
        public LessThanAttribute(int value){}
        public LessThanAttribute(uint value){}
        public LessThanAttribute(long value){}
        public LessThanAttribute(ulong value){}
        public LessThanAttribute(float value){}
        public LessThanAttribute(double value){}
        public LessThanAttribute(decimal value){}
    }
}