using SbcLibrary;
using System;

namespace SbcCore
{
    [ImplementClass(typeof(Char))]
    public struct System_Char
    {
        internal char m_value;

        public override string ToString() => new string(new char[] { m_value }, 0, 1);
        public override int GetHashCode() => m_value;
        public override bool Equals(Object obj) => obj is System_Char value && m_value == value.m_value;
    }
}