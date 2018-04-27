using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcLibrary
{
    public class SignatureParser
    {
        public string _line;
        public int _pos;

        public override string ToString() => $"{_line.Substring(0, _pos)}***{_line.Substring(_pos)}";

        public SignatureParser(string line, int pos, Node node, ArgsAttribute attr)
        {
            _line = line;
            _pos = pos;

            if (attr.HasType)
            {
                node.Type = ParseName();
            }

            if (attr.HasClassName)
            {
                node.ClassName = ParseName("::");
                Parse("::");
            }

            if (attr.HasName)
            {
                node.Name = ParseName(" ", "(");
            }

            if (attr.HasArgTypes || attr.HasArgNames)
            {
                node.Args = new List<ArgData>();

                Parse("(");

                while (!Parse(")"))
                {
                    Parse(" ");
                    Parse("params ");
                    if (attr.HasArgNames)
                    {
                        node.Args.Add(new ArgData
                        {
                            Type = ParseName(),
                            Name = ParseName(",", ")")
                        });
                    }
                    else
                    {
                        node.Args.Add(new ArgData { Type = ParseName(",", ")") });
                    }

                    Parse(",");
                    Parse(" ");
                }
            }

            if (attr.HasExtends)
            {
                Parse(" ");
                Parse("extends");
                node.Extends = ParseName();
            }
        }

        public bool Parse(string text, bool consume = true)
        {
            if (_pos + text.Length <= _line.Length && _line.Substring(_pos, text.Length) == text)
            {
                _pos += consume ? text.Length : 0;
                return true;
            }

            return false;
        }

        public string ParseName(string terminator1 = " ", string terminator2 = null)
        {
            Parse(" ");
            Parse("class ");
            Parse("native ");

            if (Parse("["))
            {
                for (; _pos < _line.Length && !Parse("]"); _pos++)
                {
                }
                Parse(" ");
            }

            Parse("class ");

            var start = _pos;
            var depth = 0;
            var result = "";

            terminator2 = terminator2 ?? terminator1;

            for (; _pos < _line.Length && (depth > 0 || (!Parse(terminator1, false) && !Parse(terminator2, false))); _pos++)
            {
                switch (_line[_pos])
                {
                    case '\'':
                        for (_pos++; _pos < _line.Length && _line[_pos] != '\''; _pos++)
                        {
                        }
                        break;
                    case '<':
                        Parse("<");
                        Parse("class ");
                        Parse("valuetype ");
                        result += _line.Substring(start, _pos - start);
                        ParseName(",", ">");
                        start = _pos;
                        break;
                    case '[':
                        depth++;
                        break;
                    case ']':
                        depth--;
                        break;
                }
            }

            return result + _line.Substring(start, _pos - start);
        }
    }
}
