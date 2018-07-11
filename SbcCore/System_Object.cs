using System;
using SbcLibrary;

namespace SbcCore
{
    [ImplementClass(typeof(Object))]
    public class System_Object
    {
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public virtual string ToString() => GetType().Name;
        public virtual bool Equals(object obj) => ReferenceEquals(this, obj);
        public virtual int GetHashCode() => InternalGetHashCode();

        [Inline]
        private int InternalGetHashCode()
            => Global.Emit<int>();

        [Inline(typeof(Type), "GetType")]
        public void InternalGetType()
            => Global.Emit(Global.Snippets.StackGet, -1, Opcode.LDA, Global.Snippets.StackSet);

        [Inline(typeof(void), ".ctor")]
        public void InternalCtor()
            => Global.Emit(Global.Snippets.StackDrop);

        [Inline]
        public static bool ReferenceEquals(object a, object b)
            => Global.Emit<bool>(Global.Snippets.StackPop, Global.Snippets.StackGet, Opcode.SUB, Opcode.ZEQ, Global.Snippets.StackSet);
    }
}
