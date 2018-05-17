using SbcLibrary;
using System;
using System.Text;

namespace SbcCore
{
    [ImplementClass("System.String")]
    public class System_String : System_Object
    {
        [Inline]
        public char get_Chars(int index)
            => Global.Emit<char>(Global.Snippets.StackPop, 
                                 Global.Snippets.StackGet, Opcode.ADD, 1, Opcode.LDA, 
                                 Global.Snippets.StackSet);

        [Inline]
        public int get_Length()
            => Global.Emit<int>(Global.Snippets.Ldlen);

        [Inline]
        private string ToStringInline() 
            => Global.Emit<string>();

        public override string ToString() => ToStringInline();

        public static char[] ctor(char[] src, int srcIdx, int len)
        {
            var str = new char[len];
            Array.Copy(src, srcIdx, str, 0, len);
            return str;
        }

        public static bool op_Equality(string a, string b) => Compare(a, b) == 0;

        public static int Compare(string a, string b)
        {
            int aLen = a.Length, bLen = b.Length, comp = 0;

            for (int i = 0; i < aLen && i < bLen && comp == 0; i++)
            {
                comp = a[i] - b[i];
            }

            return comp == 0 ? aLen - bLen : comp;
        }

        public static string Format(string format, object arg1)
            => new StringBuilder().AppendFormat(format, new[] { arg1 }).ToString();

        public static string Format(string format, object arg1, object arg2)
            => new StringBuilder().AppendFormat(format, new[] { arg1, arg2 }).ToString();

        public static string Format(string format, object arg1, object arg2, object arg3)
            => new StringBuilder().AppendFormat(format, new[] { arg1, arg2, arg3 }).ToString();

        public static string Format(string format, params object[] args)
            => new StringBuilder().AppendFormat(format, args).ToString();

        public static String Concat(String str0, String str1)
            => new StringBuilder(str0.Length + str1.Length)
                         .Append(str0).Append(str1).ToString();

        public static String Concat(String str0, String str1, String str2)
            => new StringBuilder(str0.Length + str1.Length + str2.Length)
                         .Append(str0).Append(str1).Append(str2).ToString();

        public static String Concat(String str0, String str1, String str2, String str3)
            => new StringBuilder(str0.Length + str1.Length + str2.Length + str2.Length)
                         .Append(str0).Append(str1).Append(str2).Append(str3).ToString();

        public static String Concat(params String[] values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
                sb.Append(values[i]);
            return sb.ToString();
        }

        public static String Concat(object arg0)
            => arg0.ToString();

        public static String Concat(object arg0, object arg1)
            => new StringBuilder().Append(arg0).Append(arg1).ToString();

        public static String Concat(object arg0, object arg1, object arg2)
            => new StringBuilder().Append(arg0).Append(arg1).Append(arg2).ToString();

        public static String Concat(object arg0, object arg1, object arg2, object arg3)
            => new StringBuilder().Append(arg0).Append(arg1).Append(arg2).Append(arg3).ToString();

        public static String Concat(params object[] args)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
                sb.Append(args[i]);
            return sb.ToString();
        }
        //public static String Concat<T>(IEnumerable<T> values);
        //public static String Concat(IEnumerable<String> values);

    }
}
