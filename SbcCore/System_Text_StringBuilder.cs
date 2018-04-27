using SbcLibrary;
using System;

namespace SbcCore
{
    [Implement("System.Text.StringBuilder")]
    public class System_Text_StringBuilder
    {
        private int _length;
        private char[] _data = new char[32];

        [Implement("System.Text.StringBuilder System.Text.StringBuilder::AppendFormat(string format,params object[] args)")]
        public System_Text_StringBuilder AppendFormat(string format, params object[] args)
        {
            for (int i = 0, length = format.Length, start; i < length;)
            {
                var c0 = format[i];
                var c1 = i + 1 < length ? format[i + 1] : '\0';
                var c2 = i + 2 < length ? format[i + 2] : '\0';

                if ((c0 == '{' && c1 == '{') || (c0 == '}' && c1 == '}'))
                {
                    Append(format, i, 1);
                    i += 2;
                }
                else if (c0 == '{' && c1 >= '0' && c1 <= '9' && c2 == '}')
                {
                    var arg = args[c1 - '0'].ToString();
                    Append(arg, 0, arg.Length);
                    i += 3;
                }
                else
                {
                    for (start = i++; i < length && format[i] != '{' && format[i] != '}'; i++)
                    {
                    }

                    Append(format, start, i - start);
                }
            }

            return this;
        }

        public System_Text_StringBuilder Append(string text, int index, int length)
        {
            if (_data.Length < _length + length)
            {
                var olddata = _data;
                _data = new char[_data.Length * 2 + length];
                Array.Copy(olddata, 0, _data, 0, olddata.Length);
            }

            for (int i = 0; i < length; i++)
            {
                _data[_length++] = text[index + i];
            }

            return this;
        }

        public override string ToString()
            => new string(_data, 0, _length);
    }
}
