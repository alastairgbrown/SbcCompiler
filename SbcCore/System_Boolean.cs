using SbcLibrary;
using System;

namespace SbcCore
{
    [ImplementClass(typeof(Boolean))]
    struct System_Boolean
    {
        internal bool m_value;

        public override string ToString() => m_value ? "True" : "False";
        public override int GetHashCode() => m_value ? 0 : 1;
        public override bool Equals(Object obj) => obj is System_Boolean value && m_value == value.m_value;
    }
}
