using SbcLibrary;
using System;
using System.Text;

namespace SbcCore
{
    [ImplementClass(typeof(StringBuilder))]
    public class System_Text_StringBuilder
    {
        private int _length;
        private char[] _data;

        public System_Text_StringBuilder() => _data = new char[Global.Config.HeapGranularity - 2];
        public System_Text_StringBuilder(int capacity) => _data = new char[capacity];
        public override string ToString() => new string(_data, 0, _length);
        public int Length => _length;
        public char get_Chars(int index) => _data[index];
        public string Detach() => _data.ConvertToString(_length);
        public System_Text_StringBuilder Append(object text) => Append(text.ToString());
        public System_Text_StringBuilder Append(int text) => Append(text.ToString());
        public System_Text_StringBuilder Append(string text) => Append(text, 0, text.Length);
        public System_Text_StringBuilder AppendLine() => Append(Environment.NewLine);
        public System_Text_StringBuilder AppendLine(string value) => Append(value).Append(Environment.NewLine);

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
                    Append(args[c1 - '0']);
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

        public char this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public System_Text_StringBuilder Append(char text)
        {
            EnsureCapacity(_length + 1);
            _data[_length++] = text;
            return this;
        }


        private void EnsureCapacity(int size)
        {
            if (_data.Length < size)
            {
                var oldData = _data;
                _data = new char[_data.Length * 2 < size ? size : _data.Length * 2];
                Array.Copy(oldData, 0, _data, 0, oldData.Length);
            }
        }

        public System_Text_StringBuilder Append(string text, int index, int length)
        {
            EnsureCapacity(_length + length);

            System_Array.Copy(text, index, _data, _length, length);
            _length += length;
            return this;
        }

        public System_Text_StringBuilder Remove(int index, int count)
        {
            if (count > 0)
            {
                int i = _length;
                _length -= count;
                if (index < _length)
                {
                    Array.Copy(_data, index + count, _data, index, _length - index);
                }
                Array.Clear(_data, _length, count);
            }

            return this;
        }
    }
}
