using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Int32")]
    struct System_Int32 
    {
        internal int m_value;

        public override string ToString()
        {
            var tens = 10;
            var count = 1;
            var negative = m_value < 0;
            var value = negative ? -m_value : m_value;

            while (tens <= value)
            {
                tens *= 10;
                count++;
            }

            var str = new char[(negative ? 1 : 0) + count];

            str[0] = '-';

            for (count--; count >= 0; count--)
            {
                tens = 1;
                for (int i = 0; i < count; i++)
                {
                    tens *= 10;
                }

                var digit = (int)'0';
                while (value >= tens)
                {
                    value -= tens;
                    digit++;
                }

                str[str.Length - 1 - count] = (char)digit;
            }

            return new string(str, 0, str.Length);
        }
    }
}
