using SbcLibrary;
using System;
using System.Diagnostics;

namespace SbcCore
{
    [ImplementClass(typeof(Debug))]
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
