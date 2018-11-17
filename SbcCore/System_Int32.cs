using SbcLibrary;
using System;

namespace SbcCore
{
    [ImplementClass(typeof(Int32))]
    struct System_Int32
    {
        internal int m_value;

        [Vars("tens", "count", "value", "digit", "str")]
        public override string ToString()
        {
            int tens = 10, count = 1, value = Math.Abs(m_value), digit;
            char[] str;

            while (tens <= value)
            {
                tens *= 10;
                count++;
            }

            str = new char[(m_value < 0 ? 1 : 0) + count];
            str[0] = '-';

            for (count--; count >= 0; count--)
            {
                tens = 1;
                for (int i = 0; i < count; i++)
                {
                    tens *= 10;
                }

                digit = (int)'0';
                while (value >= tens)
                {
                    value -= tens;
                    digit++;
                }

                str[str.Length - 1 - count] = (char)digit;
            }

            return str.ConvertToString(str.Length);
        }

        public override int GetHashCode() => m_value;
        public override bool Equals(object obj) => obj is System_Int32 value && m_value == value.m_value;
    }

    [ImplementClass(typeof(IntPtr))]
    struct System_IntPtr
    {
        internal int m_value;

        public override string ToString() => m_value.ToString();
        public override int GetHashCode() => m_value;
        public override bool Equals(object obj) => obj is System_IntPtr value && m_value == value.m_value;
        public static explicit operator int(System_IntPtr value) => value.m_value;
    }
}
