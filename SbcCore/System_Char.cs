using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Char")]
    public struct System_Char
    {
        internal char m_value;

        public override string ToString()
            => new string(new char[] { m_value }, 0, 1);
    }
}