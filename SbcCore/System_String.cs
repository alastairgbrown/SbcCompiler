using SbcLibrary;
using System;
using System.Text;

namespace SbcCore
{
    [Implement("System.String")]
    public class System_String : System_Object
    {
        private int _length;

        [Snippet("char System.String::get_Chars(int32)")]
        public static Snippet get_Chars => new Snippet((compiler, config)
        => compiler.Emit(compiler.Snippets.StackPop, compiler.Snippets.StackGet, Opcode.ADD, 1, Opcode.LDA, compiler.Snippets.StackSet));

        [Snippet("int32 System.String::get_Length()")]
        public static Snippet get_Length => new Snippet((compiler, config)
        => compiler.Emit(compiler.Snippets.Ldlen));

        [Implement("string System.String::ToString()")]
        public override string ToString() => ToString1();

        private string ToString1() => throw new NotImplementedException();

        [Snippet("string System.String::ToString1()")]
        public static Snippet ToString2 => new Snippet((compiler, config)
        => compiler.Emit());

        [Implement("void System.String::.ctor(char[] src, int32 srcIdx, int32 len)")]
        public static char[] ctor(char[] src, int srcIdx, int len)
        {
            var str = new char[len];
            Array.Copy(src, srcIdx, str, 0, len);
            return str;
        }

        [Implement("int System.String::Compare(string a, string b)")]
        public static int Compare(string a, string b)
        {
            int aLen = a.Length, bLen = b.Length;

            for (int i = 0; i < aLen && i < bLen; i++)
            {
                if (a[i] < b[i])
                    return -1;
                if (a[i] > b[i])
                    return 1;
            }

            return aLen < bLen ? -1 : aLen > bLen ? 1 : 0;
        }

        [Implement("string System.String::Format(string format, object arg1)")]
        public static string Format(string format, object arg1)
            => string.Format(format, new[] { arg1 });

        [Implement("string System.String::Format(string format, object arg1, object arg2)")]
        public static string Format(string format, object arg1, object arg2)
            => string.Format(format, new[] { arg1, arg2 });

        [Implement("string System.String::Format(string format, object arg1, object arg2, object arg3)")]
        public static string Format(string format, object arg1, object arg2, object arg3)
            => string.Format(format, new[] { arg1, arg2, arg3 });

        [Implement("string System.String::Format(string format, params object[] args)")]
        public static string Format(string format, params object[] args)
            => new StringBuilder().AppendFormat(format, args).ToString();
    }
}
