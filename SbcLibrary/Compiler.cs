using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SbcLibrary
{
    public class Compiler
    {
        public int Pass { get; set; } = 1;
        public int Line { get; set; }
        public string FileName { get; set; }

        public Compilation Compilation { get; } = new Compilation();
        public Config Config => Compilation.Config;
        public Dictionary<string, int> Strings { get; } = new Dictionary<string, int>();
        public SortedDictionary<string, Node> Methods { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Node> Classes { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Node> StaticFields { get; } = new SortedDictionary<string, Node>();
        public SortedDictionary<string, Label> LabelDefs { get; } = new SortedDictionary<string, Label>();
        public List<Label> LabelRefs { get; set; } = new List<Label>();
        public List<Node> IncludedNodes { get; } = new List<Node>();
        public int CurrentLabelAddress { get; set; }
        public int CurrentCtorReturn { get; set; }
        public int CurrentArgCount { get; set; }
        public int CurrentLocalCount { get; set; }
        public int CurrentCtor { get; set; }
        public int CurrentArgIndex => 1 + CurrentCtorReturn;
        public int CurrentLocalIndex => 1 + CurrentCtorReturn + CurrentArgCount;
        public int CurrentFrameSize => 1 + CurrentCtorReturn + CurrentArgCount + CurrentLocalCount;
        public Node EntryPoint { get; set; }

        public ISnippets Snippets { get; private set; }
        public Dictionary<string, Action<Compiler, Config>> SnippetsBySignature { get; private set; }

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

        private static readonly Regex StripExtra = new Regex(@"(?<keep>""(?:\\.|.)*?"")|(?<keep>\s)\s+|//.*$");

        public const string HeapInitialiseSignature = "void SbcCore.Implementations::HeapInitialise()";
        public const string NewSignature = "int32 SbcCore.Implementations::New(int32)";
        public const string CallCctors = nameof(CallCctors);

        public int CurrentAddrIdx => Config.ExecutableStart + Compilation.Opcodes.Count;
        public int CurrentStaticAddr => Config.StaticStart + Compilation.StaticData.Count;
        public int SizeOfClass(string className) => Classes["System.Type"].ClassMethodSlots.Count;

        public void ProcessFile(string fileName)
        {
            if (string.Compare(Path.GetExtension(fileName), ".dll") == 0)
                ProcessAssembly(fileName);
            else
                ProcessAsm(fileName);
        }

        public void ProcessAssembly(string fileName)
        {
            var assembly = Assembly.LoadFile(fileName);

            Snippets = (ISnippets)assembly.DefinedTypes.Single(t => t.ImplementedInterfaces.Contains(typeof(ISnippets)))
                                          .GetConstructors().Single().Invoke(new object[0]);
            SnippetsBySignature = assembly.DefinedTypes
                .SelectMany(a => a.GetProperties())
                .Select(m => new
                {
                    Attr = m.GetCustomAttributes(false).OfType<SnippetAttribute>().FirstOrDefault(),
                    Prop = m
                })
                .Where(m => m.Attr != null)
                .ToDictionary(m => m.Attr.Signature ?? m.Prop.Name, m => ((Snippet)m.Prop.GetValue(null)).Action);
        }

        public void ProcessAsm(string fileName)
        {
            Pass = 1;
            FileName = fileName;

            var stack = new Stack<Node>(new[] { new Node(this) });
            var lines = File.ReadAllLines(fileName).Select(l => StripExtra.Replace(l, "${keep}").Trim()).ToArray();

            for (Line = 0; Line < lines.Length; Line++)
            {
                if (lines[Line] == "")
                {
                    continue;
                }

                if (lines[Line] == "{")
                {
                    stack.Push(stack.Peek().Children.Last());
                    continue;
                }

                if (lines[Line] == "}")
                {
                    stack.Pop();
                    continue;
                }

                var line = new StringBuilder(lines[Line]);
                var directive = DirectiveRegex.Match(lines[Line]);
                var instruction = InstructionRegex.Match(lines[Line]);

                if (!directive.Success && !instruction.Success)
                {
                    throw new Exception($"Can't parse {line}");
                }

                var match = directive.Success ? directive : instruction;
                var attribute = (directive.Success ? Directives : Instructions)[match.Groups["Keyword"].Value];

                while (Line + 1 < lines.Length &&
                        (attribute.HasBody
                            ? lines[Line + 1] != "{"
                            : !InstructionRegex.IsMatch(lines[Line + 1]) && !DirectiveRegex.IsMatch(lines[Line + 1])))
                {
                    line.Append($" {lines[++Line]}");
                }

                var detail = attribute.Regex.Match($"{line}".Substring(match.Index + match.Length));

                if (!detail.Success)
                {
                    throw new Exception($"Can't parse {line}");
                }

                var node = new Node(this, stack.Peek(), $"{line}", attribute, match, detail);
                stack.Peek().Children.Add(node);

                if (attribute.ExecuteOnPass1)
                {
                    node.Action();
                }
            }
        }

        public void Compile()
        {
            foreach (var method in Methods.Values.ToArray())
            {
                Methods[method.Signature] = method;
            }

            EmitSource("Startup", "Startup");
            EmitSource("Initialising stack and frame");
            Emit(Config.StackStart, Opcode.PSH, Opcode.SWX);
            Emit(Config.StackStart + Config.StackSize - 1, Opcode.PSH, Opcode.SWY);
            EmitSource("Initialising heap");
            Emit(new Label(HeapInitialiseSignature), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling static constructors");
            Emit(new Label(CallCctors), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling main");
            Emit(new Label(IncludedNodes.First().Signature), Opcode.PSH, Opcode.JSR);
            EmitSource("Break");
            Emit(0, Opcode.PSH, Config.BreakAddress, Opcode.PSH, Opcode.STA);
            EmitMethodData("Startup", "Startup", 0, new ArgData[0], new ArgData[0]);

            Include(Classes["System.String"]);

            Pass = 2;
            for (var i = 0; i < IncludedNodes.Count; i++)
            {
                IncludedNodes[i].Action();
            }

            if (IncludedNodes.Any(n => n.Keyword == nameof(Node.Method) && n.Signature == NewSignature))
            {
                Methods[HeapInitialiseSignature].Action();
            }
            else
            {
                LabelRefs.Single(lr => lr.Name == HeapInitialiseSignature).RemoveCall = true;
            }

            if (!EmitCallCctors())
            {
                LabelRefs.Single(lr => lr.Name == CallCctors).RemoveCall = true;
            }

            Pass = 3;
            PatchLabels();
            PatchMetaData();
        }

        public void PatchLabels()
        {
            Pass = 3;
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
                else
                {
                    Compilation.StaticData[labelRef.Value - Config.StaticStart] = labelValue;
                }
            }
        }

        public void PatchMetaData()
        {
            Config.ExecutableSize = (Compilation.Opcodes.Count + Config.SlotsPerMemoryUnit - 1) / Config.SlotsPerMemoryUnit;
            Config.StaticSize = Compilation.StaticData.Count;
            Compilation.AddressLabels[Config.HeapPointer] = nameof(Config.HeapPointer);
            Compilation.AddressLabels[Config.OutputAddress] = nameof(Config.OutputAddress);
            Compilation.AddressLabels[Config.InputAddress] = nameof(Config.InputAddress);
            Compilation.AddressLabels[Config.InputReadyAddress] = nameof(Config.InputReadyAddress);
            Compilation.AddressLabels[Config.BreakAddress] = nameof(Config.BreakAddress);
            Compilation.AddressWritable.Add(Config.HeapPointer);
            Compilation.AddressWritable.UnionWith(Enumerable.Range(Config.StackStart, Config.StackSize));
            Compilation.AddressWritable.UnionWith(Enumerable.Range(Config.HeapStart, Config.HeapSize));
        }

        public bool EmitCallCctors()
        {
            var cctors = IncludedNodes.Where(n => n.Keyword == nameof(Node.Method) && n.Name == ".cctor").ToArray();

            if (!cctors.Any())
                return false;

            CurrentArgCount = CurrentLocalCount = CurrentCtor = CurrentCtorReturn = 0;

            EmitSource(CallCctors, CallCctors);
            Emit(Snippets.MethodPreamble);

            foreach (var method in cctors)
            {
                EmitSource($"call {method.Signature}");
                Emit(new Label(method.Signature), Opcode.PSH, Opcode.JSR);
            }

            EmitSource("ret");
            Emit(Snippets.MethodReturn);
            EmitMethodData("Startup", CallCctors, 0, new ArgData[0], new ArgData[0]);

            return true;
        }

        public void EmitMethodData(string className, string signature, int ctorReturn, ArgData[] args, ArgData[] locals)
        {
            Compilation.MethodData.Add(new MethodData
            {
                ClassName = className,
                Signature = signature,
                AddrIdx = LabelDefs[signature].Value,
                Range = CurrentAddrIdx - LabelDefs[signature].Value,
                FrameItems = new[] { "M:" + signature }
                                .Concat(new[] { "CtorReturn" }.Take(ctorReturn))
                                .Concat(args.Select(x => "A:" + x.Name))
                                .Concat(locals.Select(x => "V:" + x.Name)).ToArray()
            });
        }

        public void EmitSource(string source, string label = null)
        {
            Compilation.ExecutableLines[CurrentAddrIdx] = source;

            if (!string.IsNullOrEmpty(label))
            {
                LabelDefs[label] = new Label(label, CurrentAddrIdx, true);
            }
        }

        public void EmitOpcodes(params Opcode[] opcodes)
        {
            if (Pass == 3)
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

        public void Emit(params object[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
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

                    case Snippet snippet:
                        snippet.Action(this, Config);
                        break;

                    default:
                        throw new Exception($"Can't emit {arg}");
                }
            }
        }

        public void Include(Node node)
        {
            if (!IncludedNodes.Contains(node) && node != null)
            {
                IncludedNodes.Add(node);
            }
        }

        public int Include(string value)
        {
            if (!Strings.TryGetValue(value, out var address))
            {
                LabelRefs.Add(new Label("System.String", CurrentStaticAddr));
                Compilation.StaticData.Add(0);
                Strings[value] = address = CurrentStaticAddr;
                Compilation.AddressLabels[CurrentStaticAddr] = $@"""{value}""";
                Compilation.StaticData.Add(value.Length);
                Compilation.StaticData.AddRange(value.Select(c => (int)c));
            }

            return address;
        }

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
        public string Type { get; set; }
        public string Name { get; set; }
    }
}