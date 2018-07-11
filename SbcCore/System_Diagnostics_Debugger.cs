using SbcLibrary;
using System.Diagnostics;

namespace SbcCore
{
    [ImplementClass(typeof(Debugger))]
    public static class System_Diagnostics_Debugger
    {
        [Inline]
        public static void Break()
            => Global.Emit(
                Global.Config.BreakStop, Opcode.PSH,
                Global.Config.BreakAddress, Opcode.PSH, Opcode.STA);
    }
}
