using System;
using System.Collections.Generic;
using System.Text;

namespace SbcCore
{
    public abstract class CompilerBase
    {
        public abstract int CurrentAddrIdx { get; }
        public abstract int CurrentStaticAddr { get; }
        public abstract void Emit(params object[] args);
        public abstract int SizeOfClass(string className);
    }
}
