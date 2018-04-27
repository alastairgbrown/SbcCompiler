using SbcCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SbcLibrary
{
    public class Node
    {
        public string Line { get; }
        public Match[] Match { get; }
        public Action Action { get; }
        public List<Node> Children { get; } = new List<Node>();
        public Node Parent { get; }
        public Compiler Compiler { get; }
        public Compilation Compilation => Compiler.Compilation;
        public Config Config => Compiler.Config;
        public ISnippets Snippets => Compiler.Snippets;
        public override string ToString() => Line;

        public Node(Compiler compiler)
        {
            Compiler = compiler;
        }

        public Node(Compiler compiler, Node parent, string line, ArgsAttribute attribute, params Match[] match)
        {
            Compiler = compiler;
            Line = line;
            Match = match;
            Action = (Action)attribute.Method.CreateDelegate(typeof(Action), this);
            Parent = parent;

            new SignatureParser(Line, match.Sum(m => m.Index + m.Length), this, attribute);

            foreach (var m in match)
            {
                foreach (var prop in GetType().GetProperties().Where(p => m.Groups[p.Name]?.Success == true))
                {
                    if (prop.PropertyType == typeof(string[]))
                    {
                        prop.SetValue(this, m.Groups[prop.Name].Captures.OfType<Capture>().Select(c => c.Value).ToArray());
                    }
                    else
                    {
                        prop.SetValue(this, m.Groups[prop.Name].Value);
                    }
                }
            }

            Keyword = Keyword.Substring(0, 1).ToUpper() + Keyword.Substring(1).ToLower();
        }

        bool Parse(ref int pos, string text, bool consume = true)
        {
            if (pos + text.Length <= Line.Length && Line.Substring(pos, text.Length) == text)
            {
                pos += consume ? text.Length : 0;
                return true;
            }

            return false;
        }

        string ParseName(ref int pos, string terminator1 = " ", string terminator2 = null)
        {
            Parse(ref pos, " ");
            Parse(ref pos, "class ");

            if (Parse(ref pos, "["))
            {
                for (; pos < Line.Length && !Parse(ref pos, "]"); pos++)
                {
                }
                Parse(ref pos, " ");
            }

            var start = pos;
            var depth = 0;

            terminator2 = terminator2 ?? terminator1;

            for (; pos < Line.Length && (depth > 0 || (!Parse(ref pos, terminator1, false) && !Parse(ref pos, terminator2, false))); pos++)
            {
                switch (Line[pos])
                {
                    case '\'':
                        for (pos++; pos < Line.Length && Line[pos] != '\''; pos++)
                        {
                        }
                        break;
                    case '<':
                    case '[':
                        depth++;
                        break;
                    case '>':
                    case ']':
                        depth--;
                        break;
                }
            }

            return Line.Substring(start, pos - start);
        }

        public string Label { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Extends { get; set; }
        public string Keyword { get; set; }
        public string[] Keywords { get; set; }
        public List<ArgData> Args { get; set; }
        public string[] CustomBytes { get; set; }
        public string ClassName { get; set; }
        public string Type { get; set; }
        public Node LocalsNode { get; set; }
        public bool Noclass { get; private set; }
        public List<string> ArgTypes => (Args ?? Enumerable.Empty<ArgData>()).Select(a => a.Type).ToList();
        public List<string> ArgNames => (Args ?? Enumerable.Empty<ArgData>()).Select(a => a.Name).ToList();

        public string Signature
            => $"{Correct(Type)} {Correct(ClassName ?? Parent.Name)}::{Name}" +
                (Args == null ? null : $"({string.Join(",", ArgTypes.Select(Correct))})");
        public string ClasslessSignature
            => $"{Correct(Type)} {Name})" +
                (Args == null ? null : $"({string.Join(",", ArgTypes.Select(Correct))})");

        public string Correct(string type)
            => Compiler.Classes.TryGetValue(type, out var node) ? node.Name : type;

        [Directive(HasBody = true)]
        public void Assembly()
        {
        }

        [Directive]
        public void Ver()
        {
        }

        [Directive(HasBody = true)]
        public void Property()
        {
        }

        [Directive]
        public void Get()
        {
        }

        [Directive]
        public void Set()
        {
        }

        [Directive(HasBody = true)]
        public void Event()
        {
        }

        [Directive]
        public void Addon()
        {
        }

        [Directive]
        public void Removeon()
        {
        }

        [Directive]
        public void Param()
        {
        }

        [Directive(@"((?<Keywords>private|public|protected|internal|hidebysig|static|instance|specialname|rtspecialname|virtual|valuetype|newslot) )*",
                    HasType = true, HasName = true, HasArgTypes = true, HasArgNames = true, HasBody = true, ExecuteOnPass1 = true)]
        public void Method()
        {
            if (Compiler.Pass == 1)
            {
                Compiler.Methods[Signature] = this;
            }
            else
            {
                var ctorReturn = Children.Any(c => c.Keyword == nameof(Newobj)) ? 1 : 0;
                var args = (Keywords.Contains("instance") ? new[] { new ArgData { Name = "this" } } : new ArgData[0]).Concat(Args).ToArray();
                var locals = LocalsNode?.Args.ToArray() ?? new ArgData[0];

                Compiler.Include(Parent);
                Compiler.EmitSource(Line, Signature);
                Compiler.CurrentCtorReturn = Children.Any(c => c.Keyword == nameof(Newobj)) ? 1 : 0;
                Compiler.CurrentArgCount = args.Length;
                Compiler.CurrentLocalCount = locals.Length;
                Compiler.CurrentCtor = Name == ".ctor" && ClassName != "System.String" ? 1 : 0;
                Compiler.Emit(Snippets.MethodPreamble);

                foreach (var child in Children)
                {
                    Compiler.EmitSource(child.Line, child.Label?.Insert(0, Signature));
                    child.Action();
                }

                Compiler.EmitMethodData(Correct(ClassName ?? Parent.Name), Signature, ctorReturn, args, locals);
            }
        }

        [Directive]
        public void PublicKeyToken()
        {
        }

        [Directive(@".* = \( ((?<CustomBytes>[0-9A-F]{2}) )*\)", 
                    ExecuteOnPass1 = true)]
        public void Custom()
        {
            var buffer = CustomBytes.Select(b => byte.Parse(b, NumberStyles.HexNumber)).Skip(2).ToArray();

            using (var stream = new MemoryStream(buffer))
            using (var reader = new BinaryReader(stream))
            {
                if (Line.Contains($"{nameof(NoclassAttribute)}::.ctor()"))
                {
                    Parent.Noclass = true;
                }

                if (Line.Contains($"{nameof(ConfigAttribute)}::.ctor(string, int32)"))
                {
                    var prop = Compiler.GetType().GetProperty(Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte())));
                    prop?.SetValue(Compiler, reader.ReadInt32());
                }

                if (Line.Contains($"{nameof(ImplementAttribute)}::.ctor(string)"))
                {
                    var name = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte()));

                    if (Parent.Keyword == nameof(Class))
                    {
                        Parent.Name = name;
                        Compiler.Classes[name] = Parent;
                    }

                    if (Parent.Keyword == nameof(Method))
                    {
                        var attr = new DirectiveAttribute { HasType = true, HasClassName = true, HasName = true, HasArgNames = true, HasArgTypes = true };
                        new SignatureParser(name, 0, Parent, attr);
                        Compiler.Methods[Parent.Signature] = Parent;
                    }
                }
            }
        }

        [Directive(ExecuteOnPass1 = true)]
        public void EntryPoint()
        {
            if (Compiler.Pass == 1)
            {
                Compiler.Include(Parent);
            }
        }

        [Directive]
        public void MaxStack()
        {
        }

        [Directive(@"((?<Keywords>init) )*",
                   HasArgTypes = true, HasArgNames = true, ExecuteOnPass1 = true)]
        public void Locals()
        {
            if (Compiler.Pass == 1)
            {
                Parent.LocalsNode = this;
            }
            else
            {
            }
        }

        [Directive]
        public void File()
        {
        }
        [Directive]
        public void Stackreserve()
        {
        }
        [Directive]
        public void Subsystem()
        {
        }
        [Directive]
        public void Module()
        {
        }
        [Directive]
        public void ImageBase()
        {
        }

        [Directive(@"((?<Keywords>class|static|public|private|protected|internal|initonly|assembly) )*",
                   HasType = true, HasName = true, ExecuteOnPass1 = true)]
        public void Field()
        {
            if (Compiler.Pass == 1)
            {
                if (Keywords.Contains("static"))
                {
                    Compiler.StaticFields[Signature] = this;
                }
            }
            else
            {
                var cctor = $"void {Correct(Parent.Name)}::.cctor()";

                if (Compiler.Methods.TryGetValue(cctor, out var cctorNode))
                {
                    Compiler.Include(cctorNode);
                }

                Compiler.LabelDefs[Signature] = new Label(Signature, Compiler.CurrentStaticAddr);
                Compilation.AddressWritable.Add(Compiler.CurrentStaticAddr);
                Compilation.AddressLabels[Compiler.CurrentStaticAddr] = "S:" + Name;
                Compilation.StaticData.Add(0);
            }
        }

        public Node ClassBase =>
            Name == "System.Object" ? null : Compiler.Classes[Extends == "System.ValueType" ? "System.Object" : Extends];

        List<Node> _classMethodSlots;
        public List<Node> ClassMethodSlots
        {
            get
            {
                if (_classMethodSlots == null)
                {
                    Compiler.Include(this);

                    _classMethodSlots = ClassBase?.ClassMethodSlots.ToList() ?? new List<Node>();

                    for (int i = 0; i < _classMethodSlots.Count; i++)
                    {
                        var replacement = Children.FirstOrDefault(c => c.Keyword == nameof(Method) &&
                                                                       c.ClasslessSignature == _classMethodSlots[i].ClasslessSignature);

                        if (replacement != null)
                        {
                            _classMethodSlots[i] = replacement;
                        }
                    }

                    _classMethodSlots.AddRange(Children.Where(c => c.Keyword == nameof(Method) && c.Keywords.Contains("newslot")));
                    _classMethodSlots.ForEach(Compiler.Include);
                }

                return _classMethodSlots;
            }
        }

        List<string> _classFieldSlots;
        public List<string> ClassFieldSlots =>
            _classFieldSlots ?? (_classFieldSlots =
            (ClassBase?.ClassFieldSlots ?? Enumerable.Empty<string>())
                .Concat(Children.Where(c => c.Keyword == nameof(Field)).Select(f => f.Signature)).ToList());

        [Directive(@"((?<Keywords>abstract|ansi|auto|autochar|beforefieldinit|explicit|interface|nested \w+|private|public|rtspecialname|sealed|sequential|serializable|specialname|unicode) )*",
                   HasBody = true, HasName = true, HasExtends = true, ExecuteOnPass1 = true)]
        public void Class()
        {
            if (Compiler.Pass == 1)
            {
                Compiler.Classes[Name] = this;
            }
            else if (!Noclass)
            {
                var nameString = Compiler.Include(Name);

                Compilation.AddressLabels[Compiler.CurrentStaticAddr] = "C:" + Name;
                Compilation.StaticData.Add(nameString);
                Compiler.LabelDefs[Name] = new Label(Name, Compiler.CurrentStaticAddr);

                foreach (var slot in ClassMethodSlots)
                {
                    Compiler.LabelRefs.Add(new Label(slot.Signature, Compiler.CurrentStaticAddr));
                    Compilation.AddressLabels[Compiler.CurrentStaticAddr] = "M:" + slot.Name;
                    Compilation.StaticData.Add(0);
                }
            }
        }

        [Directive]
        public void Corflags()
        {
        }

        [Instruction]
        public void Nop()
        => Compiler.Emit(Opcode.NOP);

        [Instruction(@"""(?<Value>(?:\\.|.)*?)""")]
        public void Ldstr()
        {
            var value = Regex.Replace(Value, @"\\.", m => m.Value == "\\n" ? "\n" : m.Value == "\\r" ? "\r" : m.Value.Substring(1));

            Compiler.Emit(Compiler.Include(value), Opcode.PSH, Snippets.StackPush);
        }

        [Instruction]
        public void Ldnull()
        => Compiler.Emit(0, Opcode.PSH, Snippets.StackPush);

        int GetValue()
        => Value.StartsWith("0x") ? int.Parse(Value.Substring(2), NumberStyles.HexNumber) : int.Parse(Value);

        int GetArgIndex()
        => Compiler.CurrentArgIndex +
            (Parent.ArgNames.IndexOf(Value) is var index && index >= 0 ? index : GetValue());

        int GetLocIndex()
        => Compiler.CurrentLocalIndex +
            (Parent.LocalsNode.ArgNames.IndexOf(Value) is var index && index >= 0 ? index : GetValue());

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>\w+)?", HasName = true)]
        public void Ldarg()
        => Compiler.Emit(GetArgIndex(), Opcode.LDY, Snippets.StackPush);

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>\w+)?", HasName = true)]
        public void Ldc()
        => Compiler.Emit(GetValue(), Opcode.PSH, Snippets.StackPush);

        [Instruction(@"\.(?<Value>\w+)")]
        public void Ldelem()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.ADD, 1, Opcode.LDA, Snippets.StackSet);

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Ldfld()
        => Compiler.Emit(Snippets.StackGet, Compiler.Classes[ClassName].ClassFieldSlots.IndexOf(Signature), Opcode.LDA, 
                         Snippets.StackSet);

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Ldsfld()
        {
            if (ClassName == "SbcCore.Global")
            {
                return;
            }

            Compiler.Include(Compiler.StaticFields[Signature]);
            Compiler.Emit(new Label(Signature), Opcode.PSH, Opcode.LDA, Snippets.StackPush);
        }

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>\w+)?", HasName = true)]
        public void Ldloc()
        => Compiler.Emit(GetLocIndex(), Opcode.LDY, Snippets.StackPush);

        [Instruction(@"(\.(?<Value>\d)|(\.s (?<Value>\w+)))|(?<Name>)")]
        public void Ldloca()
        => Compiler.Emit(Snippets.GetY, GetLocIndex(), Opcode.AKA, Snippets.StackPush);

        [Instruction]
        public void Ldlen()
        => Compiler.Emit(Snippets.Ldlen);

        [Instruction(@"(\.(?<Value>\d)|(\.s (V_)?(?<Value>\w+)))|(?<Name>)")]
        public void Starg()
        => Compiler.Emit(Snippets.StackPop, GetArgIndex(), Opcode.STY);

        [Instruction(@"(\.(?<Value>\d)|(\.s (V_)?(?<Value>\w+)))|(?<Name>)")]
        public void Stloc()
        => Compiler.Emit(Snippets.StackPop, GetLocIndex(), Opcode.STY);

        [Instruction(@"\.(?<Value>\w+)")]
        public void Stelem()
        => Compiler.Emit(-2, Opcode.LDX, -1, Opcode.LDX, Opcode.ADD, Opcode.LDX, Opcode.SWP, 1, Opcode.STA, -3, Opcode.AKX);

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Stfld()
        => Compiler.Emit(Snippets.StackPop2, Compiler.Classes[ClassName].ClassFieldSlots.IndexOf(Signature), Opcode.STA);

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Stsfld()
        {
            Compiler.Include(Compiler.StaticFields[Signature]);
            Compiler.Emit(Snippets.StackPop, new Label(Signature), Opcode.PSH, Opcode.STA);
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true, HasClassName = true, HasName = true, HasArgTypes = true)]
        public void Call()
        {
            if (ClassName == "SbcLibrary.Config" &&
                Cpu.ConfigProps.TryGetValue(Name.Replace("get_", ""), out var config))
            {
                Compiler.Emit(config.GetValue(Config), Opcode.PSH, Snippets.StackPush);
            }
            else if (Compiler.SnippetsBySignature.TryGetValue(Signature, out var snippet))
            {
                snippet(Compiler, Config);
            }
            else
            {
                var method = Compiler.Methods[Signature];
                Compiler.Include(method);
                Compiler.Emit(new Label(method.Signature), Opcode.PSH, Opcode.JSR);
            }
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true, HasClassName = true, HasName = true, HasArgTypes = true)]
        public void Callvirt()
        {
            if (Compiler.Methods.TryGetValue(Signature, out var method) && method.Keywords.Contains("newslot"))
            {
                var slot = Compiler.Classes[ClassName].ClassMethodSlots.IndexOf(method);

                Compiler.Emit(-Args.Count, Opcode.LDX, -1, Opcode.LDA, slot, Opcode.LDA, Opcode.JSR);
            }
            else
            {
                Call();
            }
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true)]
        public void Newarr()
        {
            var newarr = Compiler.Methods["int32 SbcCore.Implementations::Newarr(int32)"];

            Compiler.Include(newarr);
            Compiler.Emit(new Label(newarr.Signature), Opcode.PSH, Opcode.JSR);
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true, HasClassName = true, HasName = true, HasArgTypes = true)]
        public void Newobj()
        {
            if (ClassName == "System.String")
            {
                Call();
                Compiler.Emit(new Label(ClassName), Opcode.PSH, Snippets.StackGet, -1, Opcode.STA);
                return;
            }

            var classNode = Compiler.Classes[ClassName];
            var newobj = Compiler.Methods["int32 SbcCore.Implementations::Newobj(int32,int32)"];
            var ctor = Compiler.Methods[Signature];

            Compiler.Include(classNode);
            Compiler.Include(newobj);
            Compiler.Include(ctor);
            Compiler.Emit(classNode.ClassFieldSlots.Count, Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(ClassName), Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(newobj.Signature), Opcode.PSH, Opcode.JSR);
            Compiler.Emit(Snippets.StackGet, 1, Opcode.STY);
            Compiler.Emit(new Label(ctor.Signature), Opcode.PSH, Opcode.JSR);
            Compiler.Emit(1, Opcode.LDY, Snippets.StackPush);
        }

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Br()
        => Compiler.Emit(new Label(Parent.Signature + Value), Opcode.JMP);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Brtrue()
        => Compiler.Emit(Snippets.StackPop, Opcode.ZEQ, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Brfalse()
        => Compiler.Emit(Snippets.StackPop, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Beq()
        => Compiler.Emit(Snippets.StackPop2, Opcode.SUB, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bne()
        => Compiler.Emit(Snippets.StackPop2, Opcode.SUB, Opcode.ZEQ, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bgt()
        => Compiler.Emit(Snippets.StackPop2, Opcode.AGB, Opcode.ZEQ, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bge()
        => Compiler.Emit(Snippets.StackPop2, Opcode.SWP, Opcode.AGB, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Blt()
        => Compiler.Emit(Snippets.StackPop2, Opcode.SWP, Opcode.AGB, Opcode.ZEQ, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Ble()
        => Compiler.Emit(Snippets.StackPop2, Opcode.AGB, new Label(Parent.Signature + Value), Opcode.JPZ);

        [Instruction]
        public void Conv()
        {
        }

        [Instruction("",
                     HasType = true)]
        public void Box()
        {
            var classNode = Compiler.Classes[Type];
            var newobj = Compiler.Methods["int32 SbcCore.Implementations::Newobj(int32,int32)"];

            Compiler.Include(classNode);
            Compiler.Include(newobj);
            Compiler.Emit(classNode.ClassMethodSlots.Count, Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(Type), Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(newobj.Signature), Opcode.PSH, Opcode.JSR);
            Compiler.Emit(-1, Opcode.LDX, Snippets.StackGet, Opcode.STA);
            Compiler.Emit(Snippets.StackPop, Snippets.StackSet);
        }

        [Instruction]
        public void Add()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.ADD, Snippets.StackSet);

        [Instruction]
        public void Sub()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SWP, Opcode.SUB, Snippets.StackSet);

        [Instruction]
        public void Mul()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.MLT, Opcode.POP, Snippets.StackSet);

        [Instruction]
        public void Neg()
        => Compiler.Emit(0, Opcode.PSH, Snippets.StackGet, Opcode.SUB, Snippets.StackSet);

        [Instruction]
        public void Or()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.IOR, Snippets.StackSet);

        [Instruction]
        public void And()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.AND, Snippets.StackSet);

        [Instruction]
        public void Clt()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SWP, Opcode.AGB, Snippets.StackSet);

        [Instruction]
        public void Ceq()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SUB, Opcode.ZEQ, Snippets.StackSet);

        [Instruction]
        public void Cgt()
        => Compiler.Emit(Snippets.StackPop, Snippets.StackGet, Opcode.AGB, Snippets.StackSet);

        [Instruction]
        public void Dup()
        => Compiler.Emit(Snippets.StackDup);

        [Instruction]
        public void Pop()
        => Compiler.Emit(Snippets.StackDrop);

        [Instruction]
        public void Ret()
        => Compiler.Emit(Snippets.MethodReturn);

        [Instruction]
        public void Finally()
        {
        }

        [Instruction]
        public void Leave()
        {
        }
    }
}