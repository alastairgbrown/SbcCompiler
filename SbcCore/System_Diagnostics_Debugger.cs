using SbcLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcCore
{
    [ImplementClass("System.Diagnostics.Debugger")]
    public static class System_Diagnostics_Debugger
    {
        [Inline]
        public static void Break()
            => Global.Emit(
                Global.Config.BreakStop, Opcode.PSH,
                Global.Config.BreakAddress, Opcode.PSH, Opcode.STA);
    }
}
