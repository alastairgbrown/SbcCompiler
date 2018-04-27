using SbcLibrary;

namespace SbcCore
{
    public class System_Console 
    {
        [Implement("void System.Console::Write(string message)")]
        public static void Write(string message)
        {
            for (var i = 0; i < message.Length; i++)
            {
                Global.Memory[Global.Config.OutputAddress] = message[i];
            }
        }

        [Implement("void System.Console::WriteLine(string message)")]
        public static void WriteLine(string message)
        {
            Write(message);
            Write("\r\n");
        }
    }
}
