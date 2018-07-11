using SbcLibrary;
using System;
using System.Text;

namespace SbcCore
{
    [ImplementClass(typeof(String))]
    public class System_String : System_Object
    {
        static string Empty = "";

        [Inline]
        public char get_Chars(int index)
            => Global.Emit<char>(Global.Snippets.StackPop,
                                 Global.Snippets.StackGet, Opcode.ADD, 1, Opcode.LDA,
                                 Global.Snippets.StackSet);

        [Inline]
        public int get_Length()
            => Global.Emit<int>(Global.Snippets.Ldlen);

        [Inline]
        private string AsString() => Global.Emit<string>();

        public override string ToString() => AsString();
        public override bool Equals(Object obj) => obj is System_String str && Compare(AsString(), str.AsString()) == 0;

        [Vars("hash", "i", "len")]
        public override int GetHashCode()
        {
            int hash = 5381, i, len;

            for (i = 0, len = get_Length(); i < len; i++)
                hash = ((hash << 5) + hash) ^ get_Chars(i);

            return hash;
        }

        public static string InternalCtor(char[] src, int srcIdx, int len)
        {
            var str = new char[len];
            Array.Copy(src, srcIdx, str, 0, len);
            return str.ConvertToString(len);
        }

        public static bool op_Equality(string a, string b) => Compare(a, b) == 0;
        public static bool op_Inequality(string a, string b) => Compare(a, b) != 0;

        [Vars("comp", "i")]
        public static int Compare(string a, int aIndex, int aLen, string b, int bIndex, int bLen)
        {
            int comp = 0;

            for (int i = 0; i < aLen && i < bLen && comp == 0; i++)
            {
                comp = a[aIndex + i] - b[bIndex + i];
            }

            return comp == 0 ? aLen - bLen : comp;
        }

        public static int Compare(string a, string b)
            => Compare(a, 0, a.Length, b, 0, b.Length);

        public bool StartsWith(string b)
            => b.Length <= get_Length() && Compare(AsString(), 0, b.Length, b, 0, b.Length) == 0;

        public bool EndsWith(string b)
            => b.Length <= get_Length() && Compare(AsString(), get_Length() - b.Length, b.Length, b, 0, b.Length) == 0;

        public bool Contains(string b)
            => IndexOf(b) >= 0;

        [Vars("i", "len")]
        public int IndexOf(string b)
        {
            for (int i = 0, len = get_Length() - b.Length; i <= len; i++)
            {
                if (Compare(AsString(), i, b.Length, b, 0, b.Length) == 0)
                    return i;
            }

            return -1;
        }

        public static string Format(string format, object arg1)
            => new StringBuilder().AppendFormat(format, new[] { arg1 }).Detach();

        public static string Format(string format, object arg1, object arg2)
            => new StringBuilder().AppendFormat(format, new[] { arg1, arg2 }).Detach();

        public static string Format(string format, object arg1, object arg2, object arg3)
            => new StringBuilder().AppendFormat(format, new[] { arg1, arg2, arg3 }).Detach();

        public static string Format(string format, params object[] args)
            => new StringBuilder().AppendFormat(format, args).Detach();

        public static String Concat(String str0, String str1)
            => new StringBuilder(str0.Length + str1.Length).Append(str0).Append(str1).Detach();

        public static String Concat(String str0, String str1, String str2)
            => new StringBuilder(str0.Length + str1.Length + str2.Length)
                         .Append(str0).Append(str1).Append(str2).Detach();

        public static String Concat(String str0, String str1, String str2, String str3)
            => new StringBuilder(str0.Length + str1.Length + str2.Length + str2.Length)
                         .Append(str0).Append(str1).Append(str2).Append(str3).Detach();

        public static String Concat(params String[] values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
                sb.Append(values[i]);
            return sb.Detach();
        }

        public static String Concat(object arg0)
            => arg0.ToString();

        public static String Concat(object arg0, object arg1)
            => new StringBuilder().Append(arg0).Append(arg1).Detach();

        public static String Concat(object arg0, object arg1, object arg2)
            => new StringBuilder().Append(arg0).Append(arg1).Append(arg2).Detach();

        public static String Concat(object arg0, object arg1, object arg2, object arg3)
            => new StringBuilder().Append(arg0).Append(arg1).Append(arg2).Append(arg3).Detach();

        public static String Concat(params object[] args)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
                sb.Append(args[i]);
            return sb.Detach();
        }
        //public static String Concat<T>(IEnumerable<T> values);
        //public static String Concat(IEnumerable<String> values);

    }
}
