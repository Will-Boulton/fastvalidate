using System;

namespace FastValidate.Attributes
{
    public sealed partial class Validate
    {

        /// <summary>
        ///     Validator which generates a range check assertion
        /// </summary>
        public sealed class BetweenAttribute : Attribute, INumericValidation
        {
            /// <summary id="Constructor">
            ///     Create a validator for this member to assert the value is between min and max
            /// </summary>
            public BetweenAttribute(sbyte min, sbyte max) { }
            public BetweenAttribute(byte min, byte max) { }
            public BetweenAttribute(short min, short max) { }
            public BetweenAttribute(ushort min, ushort max) { }
            public BetweenAttribute(int min, int max) { }
            public BetweenAttribute(uint min, uint max) { }
            public BetweenAttribute(long min, long max) { }
            public BetweenAttribute(ulong min, ulong max) { }
            public BetweenAttribute(float min, float max) { }
            public BetweenAttribute(double min, double max) { }
            public BetweenAttribute(decimal min, decimal max) { }
        }
    }
}
