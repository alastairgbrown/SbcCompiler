using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SbcLibrary
{
    public class Compiler
    {
        public Compilation Compilation { get; } = new Compilation();
        public Config Config => Compilation.Config;
        public IDictionary<string, Label> LabelDefs { get; } = new SortedDictionary<string, Label>();
        public List<Label> LabelRefs { get; set; } = new List<Label>();
        public List<Node> IncludedNodes { get; } = new List<Node>();
        public List<Class> IncludedInterfaces { get; } = new List<Class>();
        public int CurrentLabelAddress { get; set; } = -1;
        public ArgData[] CurrentArgs { get; set; }
        public ArgData[] CurrentLocals { get; set; }
        public int CurrentFrameSize { get; set; }
        public Stack<Type> CurrentTypes { get; } = new Stack<Type>();
        public List<string> CurrentTypesDebug { get; } = new List<string>();
        public List<Assembly> Assemblies { get; } = new List<Assembly>();
        public IDictionary<string, Class> Types { get; } = new SortedDictionary<string, Class>();
        public List<Class> Classes { get; } = new List<Class>();
        public string MifFileName { get; private set; }
        public ISnippets Snippets { get; private set; }
        public Stack<int> FinallyAddresses { get; } = new Stack<int>();

        public Method Main => Classes.Single(c => c.Type.Name == "Program").GetMethod(typeof(void), "Main");
        public Method StringCtor => GetClass(typeof(string)).GetMethod(typeof(void), ".Ctor", typeof(char[]), typeof(int), typeof(int));
        public Method StringCtorMethod => GetClass(typeof(string)).GetMethod(typeof(string), "InternalCtor", typeof(char[]), typeof(int), typeof(int));
        public Method IsInst => GetClass(typeof(Type)).GetMethod(typeof(object), "IsInst", typeof(object));
        public Method Break => GetClass(typeof(Debugger)).GetMethod(typeof(void), nameof(Debugger.Break));
        public Method HeapInitialise => GetClass(typeof(Memory)).GetMethod(typeof(void), nameof(Memory.HeapInitialise));
        public Method Newarr => GetClass(typeof(Memory)).GetMethod(typeof(int), nameof(Memory.Newarr), typeof(int), typeof(int), typeof(int));
        public Method Newobj => GetClass(typeof(Memory)).GetMethod(typeof(int), nameof(Memory.Newobj), typeof(int), typeof(int));
        public Method CallCctors => GetClass(typeof(Memory)).GetMethod(typeof(void), nameof(Memory.CallCCtors));

        public int CurrentAddrIdx => Config.ExecutableStart + Compilation.Opcodes.Count;
        public int CurrentConstAddr => Config.ConstStart + Compilation.ConstData.Count;
        public int CurrentStaticAddr => Config.StaticStart + Compilation.StaticDataCount;

        public Type Correct(Type type)
            => type != null && Types.TryGetValue(type.Id(), out var c) ? c.Implements : type;

        public void ProcessFile(string fileName)
        {
            if (string.Compare(Path.GetExtension(fileName), ".dll", true) == 0 ||
                string.Compare(Path.GetExtension(fileName), ".exe", true) == 0)
                Assemblies.Add(Assembly.LoadFile(fileName));
            else if (string.Compare(Path.GetExtension(fileName), ".mif", true) == 0)
                MifFileName = fileName;
        }

        public void ProcessAssemblies()
        {
            var types = Assemblies.SelectMany(a => a.DefinedTypes).ToList();

            Snippets = (ISnippets)types.FirstOrDefault(
                        t => t.ImplementedInterfaces.Contains(typeof(ISnippets)))?.GetConstructors().Single().Invoke(new object[0]);
            foreach (var type in types)
                new Class(this, type);

            Classes.ForEach(c => c.LoadMethods());

            //var dss = new Dictionary<string, string>();
            //var dsst = typeof(Dictionary<string, string>);
            //var method = dsst.GetTypeInfo().DeclaredMethods.Single(m => m.Name == "Insert");
            //var ilReader = new ILReader(method.GetMethodBody(), method.Module, dsst.GenericTypeArguments, null);
            //var instructions = ilReader.Instructions.ToArray();
        }

        public void Compile()
        {
            ProcessAssemblies();

            Global.Compiler = this;
            Global.Config = Config;

            Include(GetClass(typeof(string)));
            Include(GetClass(typeof(int)));
            Include(Main);

            EmitSource("Initialising stack and frame", "Startup");
            Emit(Config.StackStart, Opcode.PSH, Opcode.POX);
            Emit(Config.StackStart + Config.StackSize - 1, Opcode.PSH, Opcode.POY);
            EmitSource("Initialising heap");
            Emit(new Label(HeapInitialise, this), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling static constructors");
            Emit(new Label(CallCctors, this), Opcode.PSH, Opcode.JSR);
            EmitSource("Calling main");
            Emit(new Label(Main, this), Opcode.PSH, Opcode.JSR);
            EmitSource("Break");
            Emit(Break.Action);
            EmitMethodData("Startup", "Startup.Startup", "Startup", null, new ArgData[0], new ArgData[0]);

            for (var i = 0; i < IncludedNodes.Count; i++)
                IncludedNodes[i].GenerateExecutable();

            if (IncludedNodes.Contains(Newobj))
                HeapInitialise.GenerateExecutable();
            else
                LabelRefs.Single(lr => lr.Name == HeapInitialise.Id).RemoveCall = true;

            if (IncludedNodes.OfType<Method>().Any(m => m.Name == ".cctor"))
                CallCctors.GenerateExecutable();
            else
                LabelRefs.Single(lr => lr.Name == CallCctors.Id).RemoveCall = true;

            Config.ExecutableSize = (Compilation.Opcodes.Count + Config.SlotsPerMemoryUnit - 1) / Config.SlotsPerMemoryUnit;
            Config.ConstStart = Config.ExecutableStart + Config.ExecutableSize;
             
            foreach (var node in IncludedNodes)
                node.GenerateConstData();

            Config.ConstSize = Compilation.ConstData.Count;
            Config.StaticSize = Compilation.StaticDataCount;
            LabelDefs.Add(new Label("Config.StaticSize", Config.StaticSize));

            PatchLabels();
            Compilation.SetAddressInfo();

            if (!string.IsNullOrEmpty(MifFileName))
                Compilation.WriteMif(MifFileName);
        }

        public Compiler Label(string addressLabel)
            => this.With(c => Compilation.AddressLabels[CurrentConstAddr] = addressLabel);

        public Compiler ConstDef(string name)
            => this.Assert(CurrentConstAddr > 0).With(c => LabelDefs.Add(new Label(name, null, CurrentConstAddr)));

        public int ConstRef(string name, object owner, int offset = 0)
        {
            Debug.Assert(CurrentConstAddr > 0);
            LabelRefs.Add(new Label(name, owner, CurrentConstAddr + offset));
            return 0;
        }

        public void PatchLabels()
        {
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
                    Debug.Assert(labelRef.Value >= Config.ConstStart && labelRef.Value < Config.ConstStart + Config.ConstSize);
                    Compilation.ConstData[labelRef.Value - Config.ConstStart] = labelValue;
                }
            }
        }

        public void EmitCallCctors()
        {
            foreach (var method in IncludedNodes.OfType<Method>().Where(m => m.Name == ".cctor"))
            {
                EmitSource($"call {method.Signature}");
                Emit(new Label(method, this), Opcode.PSH, Opcode.JSR);
            }
        }

        public void EmitClassData(string nameSpace, string className, int address, ArgData[] fields)
        {
            Compilation.ClassData.Add(new ClassData
            {
                NameSpace = nameSpace,
                ClassName = className,
                Address = address,
                Fields = fields
            });
        }

        public void EmitMethodData(string nameSpace, string className, string signature, string id, ArgData[] args, ArgData[] locals)
        {
            Compilation.MethodData.Add(new MethodData
            {
                NameSpace = nameSpace,
                ClassName = className,
                Signature = signature,
                AddrIdx = LabelDefs[id ?? signature].Value,
                Range = CurrentAddrIdx - LabelDefs[id ?? signature].Value,
                FrameItems = new[] { new FrameItem { Name = signature, Type = "Object", Cat = "M" } }
                    .Concat(args.SelectMany(x => x.Elements.Cat("A")))
                    .Concat(locals.SelectMany(x => x.Elements.Cat("V"))).ToArray()
            });
        }

        public Compiler EmitLabelDef(string label, Node owner = null, bool isMethod = false)
        {
            LabelDefs.Add(new Label(label, owner, CurrentAddrIdx, true));
            return this;
        }

        public Compiler EmitSource(string source, string label = null, bool isMethod = false, Node owner = null)
        {
            Compilation.ExecutableLines[CurrentAddrIdx] = new SourceData
            {
                Source = source,
                Stack = CurrentTypes.Any() ? CurrentTypes.Select(t => t.FullName).ToArray() : null,
                IsMethod = isMethod
            };

            if (!string.IsNullOrEmpty(label))
            {
                EmitLabelDef(label, owner, isMethod);
            }

            CurrentTypesDebug.Add($"{source} = {string.Join(",", CurrentTypes)}");
            return this;
        }

        public void EmitOpcodes(params Opcode[] opcodes)
        {
            if (CurrentLabelAddress >= 0)
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
            foreach (var arg in args.Where(a => a.Arg != null))
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

                    case EmitArg[] emitargs:
                        Emit(emitargs);
                        break;

                    default:
                        throw new Exception($"Can't emit {arg}");
                }
            }

            return this;
        }


        public Compiler Include(Node node)
        {
            if (node != null && !IncludedNodes.Contains(node) && (node as Method)?.MethodBase.IsAbstract != true)
            {
                Debug.Assert(node.ToString() != "SbcLibrary.Class");
                Debug.Assert(!(node is Method method && method.Owner.Type.IsGenericTypeDefinition));
                IncludedNodes.Add(node);
                node.OnInclude();
            }

            return this;
        }

        public void IncludeString(string value)
        {
            var label = $@"""{value}""";

            if (LabelDefs.ContainsKey(label))
                return;

            Compilation.ConstData.Add(ConstRef(typeof(string).Id(), this));
            ConstDef(label).Label(label);
            Compilation.ConstData.Add(value.Length);
            Compilation.ConstData.AddRange(value.Select(c => (int)c));
        }

        public Class GetClass(Type type, Type implements = null)
        {
            if (!Types.TryGetValue(type.Id(), out var result))
            {
                if (type.IsArray)
                {
                    return GetClass(typeof(Array));
                }

                var classCount = Classes.Count;

                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var generic = type.GetGenericTypeDefinition();
                    var specific = GetClass(generic).Type.MakeGenericType(type.GenericTypeArguments);

                    result = new Class(this, specific.GetTypeInfo(), type);
                }
                else
                {
                    result = new Class(this, type.GetTypeInfo(), implements);
                }

                for (int i = classCount; i < Classes.Count; i++)
                {
                    Classes[i].LoadMethods();
                    Include(Classes[i]);
                }
            }

            return result;
        }

        public List<FrameItem> GetElements(Type type)
            => type == typeof(void)
                ? new List<FrameItem>()
                : type.IsArray
                    ? new List<FrameItem> { new FrameItem { Type = type.FullName } }
                    : GetClass(type).Elements;

        public int PopulateArgData(ArgData[] args, int offset = 0)
        {
            foreach (var arg in args)
            {
                arg.Offset = offset;
                arg.Elements = GetElements(arg.Type).Select(f => new FrameItem(f)).ToList().Prefix(arg.Name);
                offset += arg.Elements.Count;
            }

            return offset;
        }

        public Type TypePeek(int skip = 0)
            => Correct(CurrentTypes.Skip(skip).First());

        public Compiler TypePop()
            => this.With(c => CurrentTypes.Pop());

        public Compiler TypePush(Type type, int popCount = 0)
        {
            for (int i = 0; i < popCount; i++)
                CurrentTypes.Pop();

            if (type != null)
                CurrentTypes.Push(type);

            return this;
        }

        public bool TypesAreFloats => TypePeek(0) == typeof(float) && TypePeek(1) == typeof(float)
                                            ? true
                                            : TypePeek(0) != typeof(float) && TypePeek(1) != typeof(float)
                                                ? false
                                                : throw new Exception("mix of types");
    }

    public class MetadataBase
    {
        public string NameSpace { get; set; }
        public string ClassName { get; set; }
    }

    public class ClassData : MetadataBase
    {
        public int Address { get; set; }
        public ArgData[] Fields { get; set; }
    }

    public class MethodData : MetadataBase
    {
        public string Signature { get; set; }
        public int AddrIdx { get; set; }
        public int Range { get; set; }
        public FrameItem[] FrameItems { get; set; }
    }

    public class FrameItem
    {
        public FrameItem(FrameItem frameItem = null)
        {
            Cat = frameItem?.Cat;
            Name = frameItem?.Name;
            Type = frameItem?.Type;
        }

        public string Cat { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class ArgData
    {
        public ArgData(string name, Type type)
        {
            Name = name;
            Type = type;
        }
        public int Offset { get; set; }
        public List<FrameItem> Elements { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }
        public override string ToString() => $"{Type.FullName} {Name}";
    }
}