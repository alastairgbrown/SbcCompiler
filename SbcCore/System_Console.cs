using SbcLibrary;
using System;

namespace SbcCore
{
    [ImplementClass(typeof(Console))]
    public class System_Console 
    {
        public static void Write(string message)
        {
            for (var i = 0; i < message.Length; i++)
            {
                Global.Memory[Global.Config.OutputAddress] = message[i];
            }
        }

        public static void WriteLine(string message)
        {
            Write(message);
            Write("\r\n");
        }
    }
}
