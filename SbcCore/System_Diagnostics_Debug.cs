using SbcLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcCore
{
    [ImplementClass("System.Diagnostics.Debug")]
    public static class System_Diagnostics_Debug
    {
        public static void Assert(bool condition)
        {
            Assert(condition, "Unspecified");
        }

        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Console.Write("ASSERT: ");
                Console.WriteLine(message);
                Global.Memory[Global.Config.BreakAddress] = Global.Config.BreakAssert;
            }
        }
    }
}
