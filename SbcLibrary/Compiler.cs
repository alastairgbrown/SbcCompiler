using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SbcLibrary
{
    public class Compiler
    {
        public Pass Pass { get; set; } = Pass.Parse;

        public Compilation Compilation { get; } = new Compilation();
        public Config Config => Compilation.Config;
        public SortedDictionary<string, Node> Methods { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Node> Classes { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Node> TemplateClasses { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Node> StaticFields { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Label> LabelDefs { get; } = new SortedDictionary<string, Label>();
        public List<Label> LabelRefs { get; set; } = new List<Label>();
        public List<Node> IncludedNodes { get; } = new List<Node>();
        public List<Node> IncludedInterfaces { get; } = new List<Node>();
        public int CurrentLabelAddress { get; set; }
        public ArgData[] CurrentArgs { get; set; }
        public ArgData[] CurrentLocals { get; set; }
        public int CurrentFrameSize { get; set; }
        public Stack<TypeData> CurrentTypes { get; } = new Stack<TypeData>();
        public List<string> CurrentTypesDebug { get; } = new List<string>();
        public Node EntryPoint { get; set; }
        public string MifFileName { get; private set; }
        public int LabelIndex { get; set; }
        public Stack<Node> FinallyBlocks { get; } = new Stack<Node>();

        public ISnippets Snippets { get; private set; }
        public Dictionary<string, Action> Inlines { get; } = new Dictionary<string, Action>();

        private static readonly Dictionary<string, ArgsAttribute> Directives = typeof(Node).GetMethods()
            .Select(m => m.GetCustomAttributes(false).OfType<DirectiveAttribute>().FirstOrDefault()?.SetMethod(m))
            .Where(m => m != null)
            .ToDictionary(m => m.Method.Name, m => m, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ArgsAttribute> Instructions = typeof(Node).GetMethods()
            .Select(m => m.GetCustomAttributes(false).OfType<InstructionAttribute>().FirstOrDefault()?.SetMethod(m))
            .Where(m => m != null)
            .ToDictionary(m => m.Method.Name, m => m, StringComparer.OrdinalIgnoreCase);

        private static readonly Regex DirectiveRegex =
            new Regex(
                $@"^\.({string.Join("|", Directives.Keys.Select(k => $@"(?<Keyword>{k})\b\s?"))})|^[{{}}]$", RegexOptions.IgnoreCase);

        private static readonly Regex InstructionRegex =
            new Regex(
                $@"^((?<Label>\w+):\s)?({string.Join("|", Instructions.Keys.Select(k => $@"(?<Keyword>{k})\b"))})|^[{{}}]$", RegexOptions.IgnoreCase);

        public const string HeapInitialise = "void System.GC::HeapInitialise()";
        public const string New = "int32 System.GC::New(int32)";
        public const string Newarr = "int32 System.GC::Newarr(int32,int32,int32)";
        public const string Newobj = "int32 System.GC::Newobj(int32,int32)";
        public const string StringCtor = "void System.String::.ctor(char[],int32,int32)";
        public const string StringCtorMethod = "char[] System.String::ctor(char[],int32,int32)";
        public const string Break = "void System.Diagnostics.Debugger::Break()";
        public const string CallCctors = "CallCctors";
        public const string ClassKeywords =
            @"abstract|ansi|auto|autochar|beforefieldinit|explicit|interface|nested \w+|" +
            @"private|public|rtspecialname|sealed|sequential|serializable|specialname|unicode";

        private static readonly Regex StripComment = new Regex(@"(?<Keep>""(?:\\.|.)*?"")|(?<Keep>\s)\s+|//.*$", RegexOptions.Multiline);
        private static readonly Regex Custom =
            new Regex(@"\.class ((" + ClassKeywords + @")\s)+(?<Class>[\w+.]+)|" +
                      @"\.custom instance void (\[\w+\])(?<Name>[\w\.]+)::.ctor\(.*?\) = \(\s+(?<Byte>[A-Fa-f0-9]{2}\s+)+\)",
                    RegexOptions.Singleline);


        public int CurrentAddrIdx => Config.ExecutableStart + Compilation.Opcodes.Count;
        public int CurrentConstAddr => Config.ConstStart + Compilation.ConstData.Count;

        public string GetNewLabel() => $"Label{LabelIndex++}";

        public int CurrentStaticAddr => Config.StaticStart + Compilation.StaticDataCount;


        public int SizeOfClass(string className) => Classes["System.Type"].ClassMethodSlots.Count;

        public void ProcessFile(string fileName)
        {
            if (string.Compare(Path.GetExtension(fileName), ".dll") == 0)
                ProcessAssembly(fileName);
            else if (string.Compare(Path.GetExtension(fileName), ".mif") == 0)
                ProcessMifFile(fileName);
            else
                ProcessAsmFile(fileName);
        }

        private void ProcessMifFile(string fileName)
        {
            MifFileName = fileName;
        }

        public void ProcessAssembly(string fileName)
        {
            var assembly = Assembly.LoadFile(fileName);
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var typeSubst = new HashSet<Type> { typeof(int), typeof(object[]), typeof(void), typeof(char), typeof(string), typeof(object) };
            string Correct(Type type) => typeSubst.Contains(type) ? type.Name.ToLower() : type.FullName;
            object Instance(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

            foreach (var type in assembly.DefinedTypes)
            {
                var typeName = type.GetCustomAttribute<ImplementClassAttribute>()?.Name ?? type.FullName;

                foreach (var inline in type.GetMethods(flags)
                                           .Select(m => new { Attr = m.GetCustomAttribute<InlineAttribute>(), Prop = m })
                                           .Where(m => m.Attr != null))
                {
                    var signature = inline.Attr.Signature ??
                                    $"{Correct(inline.Prop.ReturnType)} {typeName}::{inline.Prop.Name}" +
                                    $"({string.Join(",", inline.Prop.GetParameters().Select(p => Correct(p.ParameterType)))})";
                    var obj = inline.Prop.IsStatic ? null : inline.Prop.DeclaringType.GetConstructors().First().Invoke(new object[0]);
                    var args = inline.Prop.GetParameters().Select(p => p.ParameterType).Select(Instance).ToArray();

                    Inlines[signature] = () => inline.Prop.Invoke(obj, args);
                }
            }

            Snippets = (ISnippets)assembly.DefinedTypes.FirstOrDefault(t => t.ImplementedInterfaces.Contains(typeof(ISnippets)))
                                          ?.GetConstructors().Single().Invoke(new object[0]) ?? Snippets;
        }

        public void ProcessAsmFile(string fileName)
        {
            var content = File.ReadAllText(fileName);
            var className = (string)null;
            var substitutions = new Dictionary<string, string> { { $"{Guid.NewGuid()}", "" } };

            string Replace(Match match)
            {
                if (match.Groups["Class"].Success)
                {
                    className = match.Groups["Class"].Value;
                    return match.Value;
                }

                var name = match.Groups["Name"].Value;
                var bytes = match.Groups["Byte"].Captures.OfType<Capture>()
                                  .Select(b => byte.Parse(b.Value, NumberStyles.HexNumber)).Skip(2).ToArray();

                using (var stream = new MemoryStream(bytes))
                using (var reader = new BinaryReader(stream))
                {
                    if (name == $"{nameof(SbcLibrary)}.{nameof(ConfigAttribute)}")
                    {
                        var prop = Config.GetType().GetProperty(Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte())));

                        if (prop?.CanWrite == true)
                            prop.SetValue(Config, reader.ReadInt32());
                    }

                    if (name == $"{nameof(SbcLibrary)}.{nameof(ImplementClassAttribute)}")
                    {
                        substitutions[className] = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte()));
                        className = null;
                    }

                    return "\n";
                }
            }

            Regex SubstitutionsRegex() =>
                new Regex($@"\b({string.Join("|", substitutions.Keys.Select(v => v.Replace(".", @"\.")))})\b",
                            RegexOptions.Compiled);

            content = StripComment.Replace(content, "${Keep}");
            content = Custom.Replace(content, Replace);
            content = SubstitutionsRegex().Replace(content, m => substitutions[m.Value]);

            ProcessAsmContent(content);
        }

        public void ProcessAsmContent(string content)
        {
            Pass = Pass.Parse;

            var stack = new Stack<Node>(new[] { new Node(this) });
            var lines = content.Split(new[] { "\r\n", "\n", "\r" }, 0).Select(l => l.Trim()).Where(l => l != "").ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "{")
                {
                    stack.Push(stack.Peek().Children.Last());
                    continue;
                }

                if (lines[i] == "}")
                {
                    stack.Pop();
                    continue;
                }

                var line = new StringBuilder(lines[i]);
                var directive = DirectiveRegex.Match(lines[i]);
                var instruction = InstructionRegex.Match(lines[i]);

                if (!directive.Success && !instruction.Success)
                {
                    throw new Exception($"Can't parse {line}");
                }

                var match = directive.Success ? directive : instruction;
                var attribute = (directive.Success ? Directives : Instructions)[match.Groups["Keyword"].Value];

                while (i + 1 < lines.Length &&
                        (attribute.HasChildren
                            ? lines[i + 1] != "{"
                            : !InstructionRegex.IsMatch(lines[i + 1]) && !DirectiveRegex.IsMatch(lines[i + 1])))
                {
                    line.Append($" {lines[++i]}");
                }

                var detail = attribute.Regex.Match($"{line}".Substring(match.Index + match.Length));

                if (!detail.Success)
                {
                    throw new Exception($"Can't parse {line}");
                }

                var node = new Node(this, stack.Peek(), $"{line}", attribute, match, detail);
                stack.Peek().Children.Add(node);

                if (attribute.ExecuteOnParse)
                {
                    node.Action();
                }
            }

            Pass = Pass.CompileExecutable;
        }

        public void Compile()
        {
            foreach (var method in Methods.Values.ToArray())
            {
                Methods[method.Signature] = method;
            }

            Global.Compiler = this;
            Global.Config = Config;
            EmitSource("Startup", "Startup");
            EmitSource("Initialising stack and frame");
            Emit(Config.StackStart, Opcode.PSH, Opcode.SWX);
            Emit(Config.StackStart + Config.StackSize - 1, Opcode.PSH, Opcode.SWY);
            EmitSource("Initialising heap");
            Emit(new Label(HeapInitialise), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling static constructors");
            Emit(new Label(CallCctors), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling main");
            Emit(new Label(IncludedNodes.First().Signature), Opcode.PSH, Opcode.JSR);
            EmitSource("Break");
            Emit(Inlines[Break]);
            EmitMethodData("Startup", "Startup", new ArgData[0], new ArgData[0]);

            Include(Classes["string"] = Classes["System.String"]);
            Include(Classes["int32"] = Classes["System.Int32"]);

            Pass = Pass.CompileExecutable;
            for (var i = 0; i < IncludedNodes.Count; i++)
            {
                IncludedNodes[i].Action();
            }

            if (IncludedNodes.Any(n => n.Keyword == nameof(Node.Method) && n.Signature == New))
            {
                Methods[HeapInitialise].Action();
            }
            else
            {
                LabelRefs.Single(lr => lr.Name == HeapInitialise).RemoveCall = true;
            }

            if (!EmitCallCctors())
            {
                LabelRefs.Single(lr => lr.Name == CallCctors).RemoveCall = true;
            }

            Config.ExecutableSize = (Compilation.Opcodes.Count + Config.SlotsPerMemoryUnit - 1) / Config.SlotsPerMemoryUnit;
            Config.StaticSize = Compilation.StaticDataCount;
            LabelDefs["Config.StaticSize"] = new Label("Config.StaticSize", Config.StaticSize);
            Config.ConstStart = Config.ExecutableStart + Config.ExecutableSize;
            Pass = Pass.CompileConst;
            foreach (var node in IncludedNodes.Where(n => n.Attribute.ExecuteOnCompileConst))
            {
                node.Action();
            }
            Config.ConstSize = Compilation.ConstData.Count;

            Pass = Pass.PatchLabels;
            PatchLabels();
            Compilation.SetAddressInfo();

            if (!string.IsNullOrEmpty(MifFileName))
                Compilation.WriteMif(MifFileName);
        }

        public void PatchLabels()
        {
            Pass = Pass.PatchLabels;
            foreach (var labelRef in LabelRefs)
            {
                if (labelRef.RemoveCall)
                {
                    CurrentLabelAddress = labelRef.Value;

                    while (Compilation.Opcodes[CurrentLabelAddress] == Opcode.PFX0)
                        EmitOpcodes(Opcode.NOP);

                    if (Compilation.Opcodes[CurrentLabelAddress] == Opcode.PSH &&
                        Compilation.Opcodes[CurrentLabelAddress + 1] == Opcode.JSR)
                        EmitOpcodes(Opcode.NOP, Opcode.NOP);

                    continue;
                }

                Debug.Assert(LabelDefs.ContainsKey(labelRef.Name));

                var labelDef = LabelDefs[labelRef.Name];
                var labelValue = labelDef.IsAddressSlot ? Config.AddrIdxToAddrSlot(labelDef.Value) : labelDef.Value;

                if (labelRef.IsAddressSlot)
                {
                    CurrentLabelAddress = labelRef.Value;
                    EmitValue(labelValue, Config.BitsPerMemoryUnit);
                }
                else if (labelRef.Value >= Config.ConstStart && labelRef.Value < Config.ConstStart + Config.ConstSize)
                {
                    Compilation.ConstData[labelRef.Value - Config.ConstStart] = labelValue;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        public bool EmitCallCctors()
        {
            var cctors = IncludedNodes.Where(n => n.Keyword == nameof(Node.Method) && n.Name == ".cctor").ToArray();

            if (!cctors.Any())
                return false;

            CurrentArgs = CurrentLocals = new ArgData[0];

            EmitSource(CallCctors, CallCctors);
            Emit(Snippets.MethodPreamble);

            foreach (var method in cctors)
            {
                EmitSource($"call {method.Signature}");
                Emit(new Label(method.Signature), Opcode.PSH, Opcode.JSR);
            }

            EmitSource("ret");
            Emit(Snippets.MethodReturn);
            EmitMethodData("Startup", CallCctors, new ArgData[0], new ArgData[0]);

            return true;
        }

        public void EmitMethodData(string className, string signature, ArgData[] args, ArgData[] locals)
        {
            Compilation.MethodData.Add(new MethodData
            {
                ClassName = className,
                Signature = signature,
                AddrIdx = LabelDefs[signature].Value,
                Range = CurrentAddrIdx - LabelDefs[signature].Value,
                FrameItems = new[] { "M:" + signature }
                                .Concat(args.SelectMany(x => Enumerable.Range(0,x.ElementSize).Select(i => "A:" + x.Name)))
                                .Concat(locals.SelectMany(x => Enumerable.Range(0, x.ElementSize).Select(i => "L:" + x.Name))).ToArray()
            });
        }

        public void EmitSource(string source, string label = null)
        {
            Compilation.ExecutableLines[CurrentAddrIdx] = source;

            if (!string.IsNullOrEmpty(label))
            {
                LabelDefs[label] = new Label(label, CurrentAddrIdx, true);
            }

            CurrentTypesDebug.Add($"{source} = {string.Join(",", CurrentTypes)}");
        }

        public void EmitOpcodes(params Opcode[] opcodes)
        {
            if (Pass == Pass.PatchLabels)
            {
                foreach (var opcode in opcodes)
                {
                    Compilation.Opcodes[CurrentLabelAddress++] = opcode;
                }
            }
            else
            {
                Compilation.Opcodes.AddRange(opcodes);
            }
        }

        public void EmitValue(int value, int bits)
            => EmitOpcodes(Enumerable.Range(0, (bits + Config.PfxBits - 1) / Config.PfxBits).Select(i => (Opcode)((value >> (Config.PfxBits * i)) & ((1 << Config.PfxBits) - 1))).Reverse().ToArray());

        public void EmitValue(int value)
            => EmitValue(value, 33 - Enumerable.Range(0, 31).TakeWhile(e => ((value & (1 << (31 - e))) == 0) == (value >= 0)).Count());

        public Compiler Emit(params EmitArg[] args)
        {
            foreach (var arg in args)
            {
                switch (arg.Arg)
                {
                    case Label label:
                        label.IsAddressSlot = true;
                        label.Value = Compilation.Opcodes.Count;
                        LabelRefs.Add(label);
                        EmitValue(0, Config.BitsPerMemoryUnit);
                        break;

                    case Opcode opcode:
                        EmitOpcodes(opcode);
                        break;

                    case int number:
                        EmitValue(number);
                        break;

                    case Action action:
                        action();
                        break;

                    default:
                        throw new Exception($"Can't emit {arg}");
                }
            }

            return this;
        }


        public Compiler Include(Node node)
        {
            if (!IncludedNodes.Contains(node) && node != null)
            {
                IncludedNodes.Add(node);

                if (node.IsInterface)
                    IncludedInterfaces.Add(node);
            }

            return this;
        }

        public void IncludeString(Node node)
        {
            Debug.Assert(Pass == Pass.CompileConst);

            if (LabelDefs.ContainsKey(node.StringLabel))
                return;

            LabelRefs.Add(new Label("System.String", CurrentConstAddr));
            Compilation.ConstData.Add(0);

            LabelDefs.Add(node.StringLabel, new Label(node.StringLabel, CurrentConstAddr));
            Compilation.AddressLabels[CurrentConstAddr] = node.StringLabel;
            Compilation.ConstData.Add(node.StringValue.Length);
            Compilation.ConstData.AddRange(node.StringValue.Select(c => (int)c));
        }

        public Node GetClass(TypeData name)
        {
            if (Classes.TryGetValue(name, out var classNode))
                return classNode;

            if (!TemplateClasses.TryGetValue(name.Name, out classNode))
                throw new Exception($"Can't find class '{name.Name}'");

            ProcessAsmContent(classNode.ClassTemplate(name.Args));

            if (Classes.TryGetValue(name, out classNode))
                return classNode;

            throw new Exception($"Can't find class '{name}'");
        }

        public int GetElementSize(TypeData type)
            => type == null || type.IsSystemType || type.Suffix != null
                    ? 1
                    : type.Name == "void"
                        ? 0
                        : GetClass(type).ElementSize;

        public int PopulateArgData(IEnumerable<ArgData> args, int offset = 0)
        {
            foreach (var arg in args)
            {
                arg.Offset = offset;
                arg.ElementSize = GetElementSize(arg.Type);
                offset += arg.ElementSize;
            }

            return offset;
        }

        public Compiler TypePop()
        {
            CurrentTypes.Pop();
            return this;
        }

        public Compiler TypePush(TypeData type)
        {
            CurrentTypes.Push(type);
            return this;
        }
    }

    public enum Pass
    {
        Parse,
        CompileExecutable,
        CompileConst,
        PatchLabels
    }

    public class ClassData
    {
        public string Name { get; set; }
        public List<ArgData> Fields { get; set; }
    }

    public class MethodData
    {
        public string ClassName { get; set; }
        public string Signature { get; set; }
        public int AddrIdx { get; set; }
        public int Range { get; set; }
        public string[] FrameItems { get; set; }
    }

    public class ArgData
    {
        public int Offset { get; set; }
        public int ElementSize { get; set; }
        public string Signature { get; set; }
        public string Name { get; set; }
        public TypeData Type { get; set; }
    }
}