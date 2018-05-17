using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SbcLibrary
{
    public class Node
    {
        public string Line { get; }
        public string SignatureSource { get; }
        public ArgsAttribute Attribute { get; }
        public Action Action { get; }
        public List<Node> Children { get; }
        public Node Parent { get; }
        public Compiler Compiler { get; }
        public string Label { get; set; }
        public TypeData Name { get; set; }
        public string Value { get; set; }
        public List<TypeData> Extensions { get; }
        public string Keyword { get; set; }
        public string[] Keywords { get; set; }
        public List<ArgData> Args { get; }
        public TypeData Type { get; set; }
        public TypeData ClassName { get; set; }
        public Node LocalsNode { get; set; }
        public TypeData Extends => Extensions?.FirstOrDefault();
        public IEnumerable<TypeData> Implements => Extensions?.Skip(1);
        public Compilation Compilation => Compiler.Compilation;
        public Config Config => Compiler.Config;
        public ISnippets Snippets => Compiler.Snippets;
        public bool IsInterface => Keyword == nameof(Class) && Keywords.Contains("interface");
        public bool IsValueType => Keyword == nameof(Class) && Extends == "System.ValueType";
        public int ElementSize => IsValueType ? ClassFieldSlots.Sum(fs => fs.ElementSize) : 1;
        public Node MethodNode => Keyword == nameof(Method) ? this : Parent;
        public string GlobalLabel => MethodNode.Signature + Label;
        public string BranchLabel => MethodNode.Signature + Value;
        public List<TypeData> ArgTypes => (Args ?? Enumerable.Empty<ArgData>()).Select(a => a.Type).ToList();
        public List<string> ArgNames => (Args ?? Enumerable.Empty<ArgData>()).Select(a => a.Name).ToList();
        public override string ToString() => Line;
        public string StringValue => $@"{Name}";
        public string StringLabel => $@"""{Name}""";
        public string Signature
            => $"{Type} {ClassName ?? Parent.Name}::{Name}" +
                (Attribute.HasArgTypes ? $"({string.Join(",", ArgTypes)})" : null);
        public string ClasslessSignature
            => $"{Type} {Name}" +
                (Attribute.HasArgTypes ? $"({string.Join(",", ArgTypes)})" : null);

        public Node(Compiler compiler)
        {
            Compiler = compiler;
            Children = new List<Node>();
        }

        public Node(Compiler compiler, Node parent, string line, ArgsAttribute attribute, params Match[] match)
        {
            Compiler = compiler;
            Line = line;
            SignatureSource = Line.Substring(match.Sum(m => m.Index + m.Length));
            Attribute = attribute;
            Action = (Action)attribute.Method.CreateDelegate(typeof(Action), this);
            Parent = parent;
            Children = Attribute.HasChildren ? new List<Node>() : null;
            Extensions = Attribute.HasExtensions ? new List<TypeData>() : null;
            Args = Attribute.HasArgTypes || Attribute.HasArgNames ? new List<ArgData>() : null;

            new SignatureParser(SignatureSource).Parse(this, attribute);

            foreach (var m in match)
            {
                foreach (var prop in GetType().GetProperties().Where(p => m.Groups[p.Name]?.Success == true))
                {
                    if (prop.PropertyType == typeof(string[]))
                        prop.SetValue(this, m.Groups[prop.Name].Captures.OfType<Capture>().Select(c => c.Value).ToArray());
                    else
                        prop.SetValue(this, m.Groups[prop.Name].Value);
                }
            }

            Keyword = Keyword.Substring(0, 1).ToUpper() + Keyword.Substring(1).ToLower();
            Label = Label ?? Compiler.GetNewLabel();
        }

        [Directive(HasChildren = true)]
        public void Assembly()
        {
        }

        [Directive]
        public void Ver()
        {
        }

        [Directive]
        public void Hash()
        {
        }

        [Directive(HasChildren = true)]
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

        [Directive(HasChildren = true)]
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

        [Directive]
        public void Override()
        {
        }

        [Directive(@"((?<Keywords>assembly|private|public|protected|internal|hidebysig|static|instance|specialname|rtspecialname|abstract|virtual|final|valuetype|newslot) )*",
                    HasType = true, HasName = true, HasArgTypes = true, HasArgNames = true, HasChildren = true, ExecuteOnParse = true)]
        public void Method()
        {
            if (Compiler.Pass == Pass.Parse)
            {
                Compiler.Methods[Signature] = this;
            }
            else
            {
                Compiler.CurrentArgs = (Keywords.Contains("instance") ? new[] { new ArgData { Name = "this", Type = "System.Object" } } : new ArgData[0])
                                            .Concat(Args).ToArray();
                Compiler.CurrentLocals = LocalsNode?.Args.ToArray() ?? new ArgData[0];
                Compiler.CurrentTypes.Clear();
                Compiler.CurrentTypesDebug.Clear();

                Compiler.CurrentFrameSize = Compiler.PopulateArgData(Compiler.CurrentArgs, 1);
                Compiler.CurrentFrameSize = Compiler.PopulateArgData(Compiler.CurrentLocals, Compiler.CurrentFrameSize);

                Compiler.Include(Parent);
                Compiler.EmitSource(Line, Signature);
                Compiler.Emit(Snippets.MethodPreamble);
                EmitBlock();
                Compiler.EmitMethodData(
                    ClassName ?? Parent.Name,
                    Signature,
                    Compiler.CurrentArgs.ToArray(),
                    Compiler.CurrentLocals.ToArray());
            }
        }

        public void EmitBlock()
        {
            foreach (var child in Children)
            {
                Compiler.EmitSource(child.Line, child.GlobalLabel);
                child.Action();
            }
        }

        [Directive]
        public void PublicKeyToken()
        {
        }

        [Directive(ExecuteOnParse = true)]
        public void EntryPoint()
        {
            if (Compiler.Pass == Pass.Parse)
            {
                Compiler.Include(Parent);
            }
        }

        [Directive]
        public void MaxStack()
        {
        }

        [Directive(@"((?<Keywords>init) )*",
                   HasArgTypes = true, HasArgNames = true, ExecuteOnParse = true)]
        public void Locals()
        {
            if (Compiler.Pass == Pass.Parse)
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
                   HasType = true, HasName = true, ExecuteOnParse = true)]
        public void Field()
        {
            if (Compiler.Pass == Pass.Parse && Keywords.Contains("static"))
            {
                Compiler.StaticFields[Signature] = this;
            }
            else if (Compiler.Pass == Pass.CompileExecutable)
            {
                Debug.Assert(Keywords.Contains("static"));

                var cctor = $"void {Parent.Name}::.cctor()";

                if (Compiler.Methods.TryGetValue(cctor, out var cctorNode))
                {
                    Compiler.Include(cctorNode);
                }

                Compiler.LabelDefs[Signature] = new Label(Signature, Compiler.CurrentStaticAddr);
                Compilation.AddressLabels[Compiler.CurrentStaticAddr] = "S:" + Name;
                Compilation.StaticDataCount++;
            }
        }

        public string ClassTemplate(List<TypeData> args)
        {
            if (Name.Args.Count != args.Count)
                throw new Exception($"{Name} has {Name.Args.Count} args, given {args.Count}");

            var nodes = new List<object> { this };
            var regexes = new List<string>();
            var map = new Dictionary<string, string>();

            for (int i = 0; i < Name.Args.Count; i++)
            {
                regexes.Add($@"\b{Name.Args[i]}\b");
                regexes.Add($@"\!{Name.Args[i]}\b");
                regexes.Add($@"\!{i}\b");
                map[Name.Args[i]] = map[$"!{Name.Args[i]}"] = map[$"!{i}"] = args[i];
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is Node node && node.Attribute.HasChildren)
                {
                    nodes.Insert(i + 1, "{");
                    nodes.InsertRange(i + 2, node.Children);
                    nodes.Insert(i + 2 + node.Children.Count, "}");
                }
            }

            var content = string.Join("\n", nodes);
            var regex = new Regex(string.Join("|", regexes));

            return regex.Replace(content, m => map[m.Value]);
        }

        public Node ClassBase
            => Name == "System.Object" ? null : Compiler.Classes[IsValueType ? "System.Object" : $"{Extends}"];

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
                    _classMethodSlots.ForEach(s => Compiler.Include(s));
                }

                return _classMethodSlots;
            }
        }

        List<ArgData> _classFieldSlots;
        public List<ArgData> ClassFieldSlots
        {
            get
            {
                if (_classFieldSlots != null)
                    return _classFieldSlots;

                _classFieldSlots = (ClassBase?.ClassFieldSlots ?? Enumerable.Empty<ArgData>()).ToList();
                var count = _classFieldSlots.Count;
                var offset = _classFieldSlots.Sum(fs => fs.ElementSize);

                _classFieldSlots.AddRange(Children.Where(c => c.Keyword == nameof(Field) && !c.Keywords.Contains("static"))
                                                  .Select(c => new ArgData { Type = c.Type, Name = c.Name, Signature = c.Signature }));
                Compiler.PopulateArgData(_classFieldSlots.Skip(count), offset);

                return _classFieldSlots;
            }
        }

        [Directive(@"((?<Keywords>" + Compiler.ClassKeywords + ") )*",
                   HasChildren = true, HasName = true, HasExtensions = true, ExecuteOnParse = true, ExecuteOnCompileConst = true)]
        public void Class()
        {
            if (Compiler.Pass == Pass.Parse)
            {
                if (Parent.Keyword == nameof(Class))
                {
                    Name.Name = $"{Parent.Name.Name}/{Name.Name}";
                }

                if (Name.Args != null && !Compiler.TemplateClasses.ContainsKey(Name.Name))
                {
                    Compiler.TemplateClasses[Name.Name] = this;
                }

                Compiler.Classes[Name] = Compiler.Classes[Name.Name] = this;
            }
            else if (Compiler.Pass == Pass.CompileExecutable)
            {
                ClassMethodSlots.ForEach(s => Compiler.Include(s));
            }
            else if (Compiler.Pass == Pass.CompileConst)
            {
                Compiler.IncludeString(this);

                Compilation.AddressLabels[Compiler.CurrentConstAddr] = "C:" + Name;
                Compilation.ConstData.Add(Compiler.LabelDefs[StringLabel].Value);
                Compiler.LabelDefs[Name] = new Label(Name, Compiler.CurrentConstAddr);

                foreach (var slot in ClassMethodSlots)
                {
                    Compiler.LabelRefs.Add(new Label(slot.Signature, Compiler.CurrentConstAddr));
                    Compilation.AddressLabels[Compiler.CurrentConstAddr] = "M:" + slot.Name;
                    Compilation.ConstData.Add(0);
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

        [Instruction(@"""(?<Value>(?:\\.|.)*?)""", ExecuteOnCompileConst = true)]
        public void Ldstr()
        {
            if (Compiler.Pass == Pass.CompileExecutable)
            {
                Name = new TypeData { Name = Regex.Replace(Value, @"\\.", m => m.Value == "\\n" ? "\n" : m.Value == "\\r" ? "\r" : m.Value.Substring(1)) };
                Compiler.TypePush(null);
                Compiler.Include(this);
                Compiler.Emit(new Label(StringLabel), Opcode.PSH, Snippets.StackPush);
            }
            else if (Compiler.Pass == Pass.CompileConst)
            {
                Compiler.IncludeString(this);
            }
        }

        [Instruction]
        public void Ldnull()
            => Compiler.Emit(0, Opcode.PSH, Snippets.StackPush).TypePush(null);

        int GetValue()
            => Value.StartsWith("0x")
                ? int.Parse(Value.Substring(2), NumberStyles.HexNumber)
                : Value.StartsWith("m")
                    ? -int.Parse(Value.Substring(1))
                    : int.Parse(Value);

        ArgData Arg
            => Compiler.CurrentArgs[Parent.ArgNames.IndexOf(Value) is var index && index >= 0 ? index : GetValue()];

        ArgData Loc
            => Compiler.CurrentLocals[Parent.LocalsNode.ArgNames.IndexOf(Value) is var index && index >= 0 ? index : GetValue()];

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>\w+)?", HasName = true)]
        public void Ldarg()
            => Compiler.Emit(Snippets.CopyFromFrameToStack(Arg));

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>-?\w+)?")]
        public void Ldc()
            => Compiler.Emit(GetValue(), Opcode.PSH, Snippets.StackPush).TypePush(null);

        [Instruction(@"(\.i.)?(\.s)?[ .](?<Value>\w+)?", HasName = true)]
        public void Ldloc()
            => Compiler.Emit(Snippets.CopyFromFrameToStack(Loc));

        [Instruction(@"(\.(?<Value>\d)|(\.s (?<Value>\w+)))|(?<Name>)")]
        public void Ldloca()
            => Compiler.TypePush(null)
                       .Emit(Snippets.GetRY, Loc.Offset, Opcode.AKA, Snippets.StackPush);

        [Instruction]
        public void Ldlen()
            => Compiler.Emit(Snippets.Ldlen);

        [Instruction(@"(\.(?<Value>\d)|(\.s (V_)?(?<Value>\w+)))|(?<Name>)")]
        public void Starg()
            => Compiler.Emit(Snippets.CopyFromStackToFrame(Arg));

        [Instruction(@"(\.(?<Value>\d)|(\.s (V_)?(?<Value>\w+)))|(?<Name>)")]
        public void Stloc()
            => Compiler.Emit(Snippets.CopyFromStackToFrame(Loc));

        TypeData ElemType => Value == "ref" ? (TypeData)"System.Object" :
                             string.IsNullOrWhiteSpace(Value) ? Type : (TypeData)"System.Int32";

        [Instruction(@"(\.(?<Value>[!]?\w+))?", HasType = true)]
        public void Ldelem()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop,
                             Compiler.GetElementSize(ElemType), Opcode.PSH, Opcode.MLT, Opcode.POP,
                             Snippets.StackPop,
                             Opcode.ADD, 1, Opcode.AKA,
                             Snippets.CopyFromMemoryToStack(ElemType));

        [Instruction(@"", HasType = true)]
        public void Ldelema()
            => Compiler.TypePop().TypePop().TypePush("System.Object")
                       .Emit(Snippets.StackPop,
                             Compiler.GetElementSize(Type), Opcode.PSH, Opcode.MLT, Opcode.POP,
                             Snippets.StackGet,
                             Opcode.ADD, 1, Opcode.AKA,
                             Snippets.StackSet);

        [Instruction(@"(\.(?<Value>[!]?\w+))?", HasType = true)]
        public void Stelem()
            => Compiler.Emit(-Compiler.GetElementSize(ElemType), Opcode.LDX,
                             Compiler.GetElementSize(ElemType), Opcode.PSH, Opcode.MLT, Opcode.POP,
                             -1 - Compiler.GetElementSize(ElemType), Opcode.LDX,
                             Opcode.ADD, 1, Opcode.AKA)
                       .Emit(Snippets.CopyFromStackToMemory(ElemType))
                       .Emit(Snippets.StackPop2).TypePop().TypePop();

        ArgData FieldData
            => Compiler.GetClass(ClassName).ClassFieldSlots.First(fs => fs.Signature == Signature);

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Ldfld()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, FieldData.Offset, Opcode.AKA)
                       .Emit(Snippets.CopyFromMemoryToStack(Type));

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Ldsfld()
        {
            if (ClassName == "SbcLibrary.Global")
                Compiler.TypePush("void");
            else
                Compiler.Include(Compiler.StaticFields[Signature])
                        .Emit(new Label(Signature), Opcode.PSH, Snippets.CopyFromMemoryToStack(Type));
        }

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Stfld()
            => Compiler.Emit(-FieldData.ElementSize, Opcode.LDX, FieldData.Offset, Opcode.AKA)
                       .Emit(Snippets.CopyFromStackToMemory(Type))
                       .Emit(Snippets.StackPop).TypePop();

        [Instruction(@"((?<Keywords>class|static|public) )*",
                     HasType = true, HasClassName = true, HasName = true)]
        public void Stsfld()
        => Compiler.Include(Compiler.StaticFields[Signature])
                   .Emit(new Label(Signature), Opcode.PSH)
                   .Emit(Snippets.CopyFromStackToMemory(Type));

        private void CallArgs(bool includesthis)
        {
            for (int i = 0; i < Args.Count + (includesthis ? 1 : 0); i++)
                Compiler.TypePop();
            Compiler.TypePush(Type);
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true, HasClassName = true, HasName = true, HasArgTypes = true)]
        public void Call()
        {
            CallArgs(Keywords?.Contains("instance") == true);

            if (ClassName == "SbcLibrary.Config" &&
                Config.GetType().GetProperty(Name.Name.Replace("get_", "")) is var config)
            {
                Compiler.Emit((int)config.GetValue(Config), Opcode.PSH, Snippets.StackPush);
            }
            else if (Compiler.Inlines.TryGetValue(Signature, out var inline))
            {
                inline();
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
                var slot = Compiler.GetClass(ClassName).ClassMethodSlots.IndexOf(method);

                CallArgs(Keywords?.Contains("instance") == true);
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
            var newarr = Compiler.Methods[Compiler.Newarr];
            var elementSize = Compiler.GetElementSize(Type);

            Compiler.Include(newarr);
            Compiler.Emit(
                0, Opcode.PSH, Snippets.StackPush, // TODO vtable
                elementSize, Opcode.PSH, Snippets.StackPush,
                new Label(newarr.Signature), Opcode.PSH, Opcode.JSR);
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
                     HasType = true, HasClassName = true, HasName = true, HasArgTypes = true)]
        public void Newobj()
        {
            CallArgs(false);

            if (Signature == Compiler.StringCtor)
            {
                // This is what we need to use here
                var method = Compiler.Methods[Compiler.StringCtorMethod];
                Compiler.Include(method);
                Compiler.Emit(new Label(method.Signature), Opcode.PSH, Opcode.JSR);

                // But we need to patch the vtable pointer to make it a string
                Compiler.Emit(new Label(ClassName), Opcode.PSH, Snippets.StackGet, -1, Opcode.STA);
                return;
            }

            var classNode = Compiler.GetClass(ClassName);
            var newobj = Compiler.Methods[Compiler.Newobj];
            var ctor = Compiler.Methods[Signature];
            var argsSize = Compiler.PopulateArgData(Args);
            var typeSize = Compiler.GetElementSize(ClassName);

            Args.ForEach(a => Compiler.TypePop());
            Compiler.TypePush(Type);
            Compiler.Include(classNode);
            Compiler.Include(ctor);

            // Insert two slots under the args - one to become 'this' for the constructor
            // the other to become the return value
            Compiler.Emit(typeSize + 1, Opcode.AKX);

            if (argsSize > 0)
                Compiler.Emit(Snippets.GetRX, Opcode.DUP, -1 - typeSize, Opcode.AKA, argsSize, Opcode.PSH, Snippets.MBD);

            if (classNode.IsValueType)
            {
                Compiler.Emit(Snippets.GetRX, -argsSize - typeSize, Opcode.AKA, -argsSize, Opcode.STX);
            }
            else
            {
                // Create space on the heap
                Compiler.Include(newobj);
                Compiler.Emit(classNode.ClassFieldSlots.Count, Opcode.PSH, Snippets.StackPush);
                Compiler.Emit(new Label(ClassName), Opcode.PSH, Snippets.StackPush);
                Compiler.Emit(new Label(newobj.Signature), Opcode.PSH, Opcode.JSR);

                Compiler.Emit(Snippets.StackPop, Opcode.DUP, -argsSize - 1, Opcode.STX, -argsSize, Opcode.STX);
            }

            Compiler.Emit(new Label(ctor.Signature), Opcode.PSH, Opcode.JSR);
        }

        [Instruction(@" ?((?<Keywords>instance) )*",
             HasType = true)]
        public void Initobj()
            => Compiler.TypePop()
                       .Emit(0, Opcode.PSH, Snippets.StackPop, Opcode.STA);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Br()
            => Compiler.Emit(new Label(BranchLabel), Opcode.JMP);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Brtrue()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Opcode.ZEQ, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Brfalse()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Beq()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.SUB, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bne()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.SUB, Opcode.ZEQ, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bgt()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.AGB, Opcode.ZEQ, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Bge()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.SWP, Opcode.AGB, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Blt()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.SWP, Opcode.AGB, Opcode.ZEQ, new Label(BranchLabel), Opcode.JPZ);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Ble()
            => Compiler.TypePop().TypePop()
                       .Emit(Snippets.StackPop2, Opcode.AGB, new Label(BranchLabel), Opcode.JPZ);

        [Instruction]
        public void Conv()
        {
        }

        [Instruction("",
                     HasType = true)]
        public void Box()
        {
            var classNode = Compiler.GetClass(Type);
            var newobj = Compiler.Methods[Compiler.Newobj];
            Compiler.TypePop().TypePush(null);
            Compiler.Include(classNode);
            Compiler.Include(newobj);
            Compiler.Emit(classNode.ClassMethodSlots.Count, Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(classNode.Name), Opcode.PSH, Snippets.StackPush);
            Compiler.Emit(new Label(newobj.Signature), Opcode.PSH, Opcode.JSR);
            Compiler.Emit(-1, Opcode.LDX, Snippets.StackGet, Opcode.STA);
            Compiler.Emit(Snippets.StackPop, Snippets.StackSet);
        }

        [Instruction]
        public void Add()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.ADD, Snippets.StackSet);

        [Instruction]
        public void Sub()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SWP, Opcode.SUB, Snippets.StackSet);

        [Instruction]
        public void Mul()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.MLT, Opcode.POP, Snippets.StackSet);

        [Instruction]
        public void Neg()
            => Compiler.Emit(0, Opcode.PSH, Snippets.StackGet, Opcode.SUB, Snippets.StackSet);

        [Instruction]
        public void Or()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.IOR, Snippets.StackSet);

        [Instruction]
        public void And()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.AND, Snippets.StackSet);

        [Instruction]
        public void Shl()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SHL, Snippets.StackSet);

        [Instruction]
        public void Shr()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SHR, Snippets.StackSet);

        [Instruction]
        public void Clt()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SWP, Opcode.AGB, Snippets.StackSet);

        [Instruction]
        public void Ceq()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.SUB, Opcode.ZEQ, Snippets.StackSet);

        [Instruction]
        public void Cgt()
            => Compiler.TypePop()
                       .Emit(Snippets.StackPop, Snippets.StackGet, Opcode.AGB, Snippets.StackSet);

        [Instruction]
        public void Dup()
        {
            var count = Compiler.GetElementSize(Compiler.CurrentTypes.Peek());

            Compiler.TypePush(Compiler.CurrentTypes.Peek());
            for (int i = 0; i < count; i++)
                Compiler.Emit(-(count - 1), Opcode.LDX, Snippets.StackPush);
        }

        [Instruction]
        public void Pop()
        {
            var count = Compiler.GetElementSize(Compiler.CurrentTypes.Pop());

            for (int i = 0; i < count; i++)
                Compiler.Emit(Snippets.StackDrop);
        }

        [Instruction]
        public void Ret()
            => Compiler.Emit(Snippets.MethodReturn);

        [Directive(HasChildren = true)]
        public void Try()
        {
            var finallyBlock = Parent.Children[Parent.Children.IndexOf(this) + 1];

            Debug.Assert(finallyBlock.Keyword == nameof(Finally));

            Compiler.FinallyBlocks.Push(finallyBlock);
            EmitBlock();
            Compiler.FinallyBlocks.Pop();
        }

        [Instruction(HasChildren = true)]
        public void Finally()
        {
            Compiler.Emit(Snippets.StackPush);
            EmitBlock();
        }

        [Instruction]
        public void Endfinally()
            => Compiler.Emit(Snippets.StackPop, Opcode.JSR);

        [Instruction(@"(\.\w+)? (?<Value>\w+)")]
        public void Leave()
        {
            foreach (var finallyBlock in Compiler.FinallyBlocks)
            {
                Compiler.Emit(new Label(MethodNode.Signature + finallyBlock.Label), Opcode.PSH, Opcode.JSR);
            }

            Compiler.CurrentTypes.Clear();
            Compiler.Emit(new Label(BranchLabel), Opcode.JMP);
        }

        [Instruction(@"\.", HasType = true)]
        public void Constrained()
        {
        }
    }
}