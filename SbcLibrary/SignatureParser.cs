using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SbcLibrary
{
    public struct SignatureParser
    {
        string _text;
        int _pos;
        List<string> _templateargs;

        public override string ToString() => $"{_text.Substring(0, _pos)}***{_text.Substring(_pos)}";

        public SignatureParser(string text)
        {
            _pos = 0;
            _text = text;
            _templateargs = new List<string>();
        }

        static Regex ArgRegex = new Regex(@"^!(\d+)$");

        public void Parse(Node node, ArgsAttribute attr)
        {
            TypeData Correct(TypeData type)
                => ArgRegex.Match(type) is var match && match.Success
                        ? node.ClassName.Args[int.Parse(match.Groups[1].Value)] : type;

            if (attr.HasType)
            {
                node.Type = ParseName();
            }

            if (attr.HasClassName)
            {
                node.ClassName = ParseName("::");
                node.Type = Correct(node.Type);
                Parse("::");
            }

            if (attr.HasName)
            {
                node.Name = ParseName(" ", "(");
            }

            if (attr.HasArgTypes || attr.HasArgNames)
            {
                Parse("(");

                while (!Parse(")"))
                {
                    Parse(" ");
                    Parse("params ");
                    if (attr.HasArgNames)
                    {
                        node.Args.Add(new ArgData
                        {
                            Type = Correct(ParseName()),
                            Name = ParseName(",", ")")
                        });
                    }
                    else
                    {
                        node.Args.Add(new ArgData { Type = Correct(ParseName(",", ")")) });
                    }

                    Parse(",");
                    Parse(" ");
                }
            }

            while (attr.HasExtensions)
            {
                Parse(" ");
                if (Parse("extends ") || Parse("implements ") || Parse(","))
                    node.Extensions.Add(ParseName(" ", ","));
                else
                    break;
            }
        }

        public bool Parse(string text, bool consume = true)
        {
            if (text != null && _pos + text.Length <= _text.Length && _text.Substring(_pos, text.Length) == text)
            {
                _pos += consume ? text.Length : 0;
                return true;
            }

            return false;
        }

        public TypeData ParseName(
            string terminator1 = " ",
            string terminator2 = null)
        {
            var type = new TypeData();

            while (true)
            {
                if (Parse("["))
                {
                    while (_pos < _text.Length && !Parse("]"))
                        _pos++;
                }
                else if (!Parse(" ") && !Parse("class ") && !Parse("valuetype ") && !Parse("native "))
                {
                    break;
                }
            }

            var start = _pos;

            while (_pos < _text.Length && !Parse(terminator1, false) && !Parse(terminator2, false))
            {
                switch (_text[_pos])
                {
                    case '\'':
                        Parse("'");
                        while (_pos < _text.Length && !Parse("'"))
                            _pos++;
                        break;
                    case '[':
                        type.Name += _text.Substring(start, _pos - start);
                        start = _pos;
                        Parse("[");
                        while (_pos < _text.Length && !Parse("]"))
                            _pos++;
                        type.Suffix += _text.Substring(start, _pos - start);
                        start = _pos;
                        break;
                    case '<':
                        type.Name += _text.Substring(start, _pos - start);
                        type.Args = new List<TypeData>();
                        Parse("<");
                        while (!Parse(">", false))
                        {
                            type.Args.Add(ParseName(",", ">"));
                            Parse(",");
                        }
                        Parse(">");
                        start = _pos;
                        break;
                    default:
                        _pos++;
                        break;
                }
            }

            type.Name += _text.Substring(start, _pos - start);
            return type;
        }
    }
}
