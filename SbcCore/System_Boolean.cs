using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Boolean")]
    struct System_Boolean
    {
        internal bool m_value;

        public override string ToString() => m_value ? "True" : "False";
    }
}
