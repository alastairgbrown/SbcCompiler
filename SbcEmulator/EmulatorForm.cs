using SbcCore;
using SbcLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SbcEmulator
{
    public partial class EmulatorForm : Form
    {
        public string[] Args { get; }
        public Cpu Cpu { get; set; }
        public Cpu LastCpu { get; set; }
        public Compilation Compilation { get; set; }
        public Config Config => Compilation.Config;

        public List<CodeLine> SourceLines { get; set; }
        public List<CodeLine> CodeLines { get; set; }
        public List<RegisterValue> RegisterValues { get; } = new List<RegisterValue>();
        public AddressSlot[] AddressesByAddrIdx { get; set; } = new AddressSlot[0];
        public FrameItem[] FrameItems { get; set; } = new FrameItem[0];
        public List<MethodData> CallStack { get; } = new List<MethodData>();
        public string GetStackType(int x)
            => Compilation.ExecutableLines.TryGetValue(Cpu.CurrentAddrIdx, out var source) && source.Stack != null && x < source.Stack.Length
                    ? source.Stack[x] : null;

        public EmulatorForm(string[] args)
        {
            Args = args;

            InitializeComponent();
        }

        private void EmulatorForm_Load(object sender, EventArgs e)
        {
            var startTime = DateTime.Now;
            var compiler = new Compiler();

            //try
            //{
            Array.ForEach(Args, compiler.ProcessFile);
            compiler.Compile();

            var compileTime = DateTime.Now - startTime;
            //}
            //catch (Exception exception)
            //{
            //    Output.Text = $@"{compiler.File}({compiler.Line + 1})\n{exception.Message}";
            //    Output.ForeColor = Color.Red;
            //}

            Compilation = compiler.Compilation;
            Cpu = new Cpu { Config = Config, PC = Config.MainEntryPoint };
            Cpu.OnSetMemory += Cpu_OnSetMemory;

            SourceLines = Compilation.ExecutableLines.Select(l => new CodeLine { AddrIdx = l.Key, Line = l.Value.Source, IsMethod = l.Value.IsMethod })
                                                     .ToList();

            var start = Config.ExecutableStart * Cpu.SlotsPerMemoryUnit;
            var size = Config.ExecutableSize * Cpu.SlotsPerMemoryUnit;
            var addressesByAddrIdx = AddressesByAddrIdx;

            Array.Resize(ref addressesByAddrIdx, start + size);
            AddressesByAddrIdx = addressesByAddrIdx;

            for (int i = 0; i < AddressesByAddrIdx.Length; i++)
            {
                AddressesByAddrIdx[i].Method = null;
                AddressesByAddrIdx[i].Line = null;
            }

            foreach (var method in Compilation.MethodData)
            {
                for (int i = 0; i < method.Range; i++)
                {
                    AddressesByAddrIdx[method.AddrIdx + i].Method = method;
                }
            }

            RegisterValues.Clear();

            foreach (var rv in Cpu.RegisterProps)
            {
                object f(Cpu cpu) => rv.Value.GetValue(cpu);
                RegisterValues.Add(new RegisterValue { Name = rv.Key, ValueFunc = f });
            }

            RegisterValues.Add(new RegisterValue { Name = nameof(Config.HeapPointer), ValueFunc = cpu => cpu.Memory[Config.HeapPointer] });

            for (int i = 0; i < 10; i++)
            {
                int x = i;
                object f(Cpu cpu) => cpu.RX - x <= Config.StackStart || cpu.RX - x >= Config.StackStart + Config.StackStart
                                        ? null
                                        : GetStackType(x) == "System.Single"
                                            ? (object)new Cpu.RegisterValue(cpu.Memory[cpu.RX - x]).Float
                                            : (object)cpu.Memory[cpu.RX - x];
                RegisterValues.Add(new RegisterValue { Name = "Stack", ValueFunc = f });
            }

            Registers.RowCount = RegisterValues.Count;

            Restart();
            SetStatus($"Compiled OK in {compileTime.TotalSeconds:N2}s", Color.Green);
        }

        private void Restart()
        {
            Cpu.RegisterProps.Values.ToList().ForEach(p => p.SetValue(Cpu, Activator.CreateInstance(p.PropertyType)));
            Cpu.Memory = Compilation.GetInitialMemory();

            LastCpu = new Cpu { Memory = Cpu.Memory.ToArray() };
            Memory.Tag = 0;
            Memory.RowCount = Config.MemorySize;
            Frame.RowCount = 0;
            Output.Text = "";

            RefreshCode();
            RefreshMenus();
        }

        private void RefreshMenus()
        {
            var classMethods = Compilation.MethodData.ToLookup(md => md.ClassName);
            var nameSpaces = Compilation.ClassData.Cast<MetadataBase>()
                                        .Concat(Compilation.MethodData.Cast<MetadataBase>())
                                        .OrderBy(i => i.NameSpace).ThenBy(i => i.ClassName)
                                        .GroupBy(i => i.NameSpace)
                                        .ToDictionary(
                                            g => g.Key, 
                                            g => g.GroupBy(i => i.ClassName).ToDictionary(c => c.Key, c => c.ToArray()));

            ClassesMenuItem.DropDownItems.Clear();

            foreach (var nameSpace in nameSpaces)
            {
                var nameSpaceMenu = new ToolStripMenuItem(nameSpace.Key);

                ClassesMenuItem.DropDownItems.Add(nameSpaceMenu);

                foreach (var className in nameSpace.Value)
                {
                    var classMenu = new ToolStripMenuItem(className.Key.Substring(nameSpace.Key.Length+1));

                    nameSpaceMenu.DropDownItems.Add(classMenu);

                    foreach (var classData in className.Value.OfType<ClassData>())
                    {
                        classMenu.DropDownItems.Add(
                            new ToolStripMenuItem("Class", null, (s, e) => SelectAddress(classData.Address)));
                    }

                    foreach (var method in className.Value.OfType<MethodData>().OrderBy(cd => cd.Signature))
                    {
                        classMenu.DropDownItems.Add(
                            new ToolStripMenuItem(method.Signature, null, (s, e) => SelectAddrIdx(method.AddrIdx)));
                    }
                }
            }
        }

        private void Cpu_OnSetMemory(object sender, KeyValuePair<int, int> e)
        {
            if (e.Key == Config.OutputAddress)
            {
                Output.Text += (char)e.Value;
                Output.SelectionLength = 0;
                Output.SelectionStart = Output.Text.Length;
                Output.ScrollToCaret();
                return;
            }

            if (e.Key == Config.BreakAddress)
            {
                throw e.Value == Config.BreakAssert ? new Exception("Assert") : new BreakException();
            }

            if (e.Key == Config.HeapPointer &&
                ((e.Value & 0xF) != 0 || e.Value < Config.HeapStart || e.Value >= Config.HeapStart + Config.HeapSize))
            {
                throw new Exception($"Invalid heap pointer value {e.Value}");
            }

            if (e.Key > Cpu.RX && e.Key < Cpu.RY)
            {
                throw new Exception($"Attempted write between Stack and frame");
            }

            if (!Compilation.AddressWritable.Contains(e.Key))
            {
                throw new Exception($"Attempted write {e.Value} to protected address {DisplayNumber(e.Key)}");
            }
        }

        public string DisplayNumber(object value)
            => value is Cpu.RegisterValue register
                 ? DisplayNumber(register.Value)
                 : value is float 
                    ? $"{value}f"
                    : (Hex.Checked && value is int 
                        ? $"{value:X}" 
                        : $"{value ?? '-'}");

        public string DisplayAddrSlot(int addrSlot)
            => $"{DisplayNumber(Config.AddrSlotToAddr(addrSlot))}.{Config.AddrSlotToSlot(addrSlot)}";

        private void RefreshCode()
        {
            var start = Config.ExecutableStart * Cpu.SlotsPerMemoryUnit;
            var size = Config.ExecutableSize * Cpu.SlotsPerMemoryUnit;

            if (AsmDetail.Checked)
            {
                CodeLines = SourceLines;
                for (int i = 0; i < SourceLines.Count; i++)
                {
                    SourceLines[i].Range = (i + 1 == SourceLines.Count ? size : SourceLines[i + 1].AddrIdx) - SourceLines[i].AddrIdx;
                }
            }
            else
            {
                var cpu = new Cpu { Memory = Cpu.Memory, Config = Config };

                CodeLines = SourceLines.ToList();
                CodeLines.ForEach(l => l.Range = 0);

                for (var i = start; i < start + size; i++)
                {
                    var opcode = (Opcode)Cpu.OpCodeAt(i);
                    var codeline = new CodeLine { AddrIdx = i, Line = $"{opcode}", Range = 1 };

                    CodeLines.Add(codeline);

                    if (PfxDetail.Checked || (int)opcode > Config.MaxPfx())
                        continue;

                    cpu.PF = cpu.RK = 0;
                    cpu.PC = Config.AddrIdxToAddr(i);
                    cpu.SLOT = Config.AddrIdxToSlot(i);
                    cpu.Run(() => cpu.CurrentOpcode > Config.MaxPfx() || cpu.PC >= Config.ExecutableStart + Config.ExecutableSize);
                    i = cpu.CurrentAddrIdx;
                    opcode = (Opcode)cpu.CurrentOpcode;
                    codeline.Range += i - codeline.AddrIdx;

                    if (opcode == Opcode.JPZ || opcode == Opcode.JMP || (opcode == Opcode.PSH && (Opcode)cpu.OpCodeAt(i + 1) == Opcode.JSR))
                        codeline.Line = $"{DisplayAddrSlot(cpu.RK)} {opcode}";
                    else
                        codeline.Line = $"{DisplayNumber(cpu.RK)} {opcode}";
                }

                CodeLines = CodeLines.OrderBy(l => l.AddrIdx).ThenBy(l => l.Range).ToList();
            }

            for (int i = 0; i < size; i++)
            {
                AddressesByAddrIdx[start + i].Line = null;
            }

            for (var i = 0; i < CodeLines.Count; i++)
            {
                var line = CodeLines[i];
                line.Index = i;
                for (int j = 0; j < line.Range && line.AddrIdx + j < AddressesByAddrIdx.Length; j++)
                {
                    AddressesByAddrIdx[line.AddrIdx + j].Line = line;
                }
            }

            Code.SuspendLayout();
            Code.RowCount = CodeLines.Count;
            Code.ResumeLayout();
            Code.Invalidate();
            RefreshState();

            if (AsmDetail.Checked)
            {
                Code.AutoResizeColumns();
            }
        }

        private void RefreshState()
        {
            SelectAddrIdx(Cpu.CurrentAddrIdx);
            Registers.Invalidate();
            Frame.Invalidate();
            Memory.Invalidate();
        }

        private void Code_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            var line = CodeLines[e.RowIndex];

            if (e.ColumnIndex == CodeBreakpoint.Index)
            {
                e.Value = AddressesByAddrIdx[line.AddrIdx].BreakPoint;
            }

            if (e.ColumnIndex == CodeAddress.Index && line.Range > 0)
            {
                e.Value = DisplayAddrSlot(Config.AddrIdxToAddrSlot(line.AddrIdx));
            }

            if (e.ColumnIndex == CodeInfo.Index)
            {
                e.Value = line.Line;
            }
        }


        private void Code_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var line = CodeLines[e.RowIndex];

            if (line.IsMethod)
                e.CellStyle.BackColor = Color.Yellow;

            if (!AsmDetail.Checked && line.Range > 0)
                e.CellStyle.ForeColor = Color.Blue;
        }

        private void Run(Func<bool> stop)
        {
            Cpu.RegisterProps.Values.ToList().ForEach(p => p.SetValue(LastCpu, p.GetValue(Cpu)));
            Array.Copy(Cpu.Memory, LastCpu.Memory, Cpu.Memory.Length);

            try
            {
                Cpu.Run(stop);
                SetStatus("");
            }
            catch (BreakException)
            {
                SetStatus("break");
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, Color.Red, $"{ex}");
            }

            RefreshFrameNames();
            RefreshState();
        }

        public void SetStatus(string text, Color? color = null, string detail = null)
        {
            Status.Text = text;
            Status.BackColor = SystemColors.Menu;
            Status.ForeColor = color ?? Color.Black;
            Status.Tag = detail;
        }

        private void RefreshFrameNames()
        {
            var stackStart = Config.StackStart;
            var stackStop = Config.StackStart + Config.StackSize;
            var framePointer = Cpu.RY;
            var frameSize = framePointer > stackStart && framePointer < stackStop ? stackStop - framePointer : 1;

            FrameItems = new FrameItem[frameSize];
            CallStack.Clear();

            for (int i = 0; i < frameSize;)
            {
                var addrIdx = i == 0
                                ? Cpu.CurrentAddrIdx
                                : Config.AddrSlotToAddrIdx(Cpu.Memory[framePointer + i]);

                if (addrIdx < 0 || addrIdx >= AddressesByAddrIdx.Length || AddressesByAddrIdx[addrIdx].Method == null)
                    break;

                var method = AddressesByAddrIdx[addrIdx].Method;

                CallStack.Add(method);
                Array.Copy(method.FrameItems, 0, FrameItems, i, Math.Min(method.FrameItems.Length, frameSize - i));

                i += method.FrameItems.Length;
            }

            Frame.Tag = framePointer;
            Frame.RowCount = FrameItems.Length;
        }

        private void Run_Click(object sender, EventArgs e)
        {
            Run(() => AddressesByAddrIdx[Cpu.CurrentAddrIdx].BreakPoint);
        }


        private void RunToMenuItem_Click(object sender, EventArgs e)
        {
            var addrIdx = CodeLines[Code.CurrentCell.RowIndex].AddrIdx;

            Run(() => AddressesByAddrIdx[Cpu.CurrentAddrIdx].BreakPoint || Cpu.CurrentAddrIdx == addrIdx);
        }

        private void StepIn_Click(object sender, EventArgs e)
        {
            var start = AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line;

            Run(() => AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line != start);
        }

        private void StepOut_Click(object sender, EventArgs e)
        {
            var start = AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line;
            var callStack = CallStack.Skip(1).ToList();

            Run(() => AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line != start &&
                        (AddressesByAddrIdx[Cpu.CurrentAddrIdx].BreakPoint ||
                         callStack.Count == 0 ||
                         callStack.Contains(AddressesByAddrIdx[Cpu.CurrentAddrIdx].Method)));
        }

        private void StepOver_Click(object sender, EventArgs e)
        {
            var start = AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line;

            Run(() => AddressesByAddrIdx[Cpu.CurrentAddrIdx].Line != start &&
                        (AddressesByAddrIdx[Cpu.CurrentAddrIdx].BreakPoint ||
                         CallStack.Count == 0 ||
                         CallStack.Contains(AddressesByAddrIdx[Cpu.CurrentAddrIdx].Method)));
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            Restart();
            SetStatus("Reset", Color.Green);
        }

        private void Memory_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            var address = (int)((DataGridView)sender).Tag + e.RowIndex;
            var item = sender == Frame && e.RowIndex < FrameItems?.Length ? FrameItems[e.RowIndex] : null;

            if (e.ColumnIndex == MemoryAddress.Index)
            {
                e.Value = DisplayNumber(address);
            }
            else if (e.ColumnIndex == MemoryValue.Index)
            {
                var value = Cpu.Memory[address];

                if (IsAddressPointingToCode(address))
                    e.Value = DisplayAddrSlot(value);
                else if (item != null && item?.Type == "System.Single")
                    e.Value = DisplayNumber(new Cpu.RegisterValue(value).Float);
                else
                    e.Value = DisplayNumber(value);
            }
            else if (e.ColumnIndex == FrameCat.Index && item != null)
            {
                e.Value = item.Cat;
            }
            else if (e.ColumnIndex == FrameName.Index && item != null)
            {
                e.Value = item.Name;
            }
            else if (e.ColumnIndex == FrameType.Index && item != null)
            {
                e.Value = item.Type;
            }
            else if (e.ColumnIndex == MemoryName.Index && sender == Memory && Compilation.AddressLabels.TryGetValue(address, out var label))
            {
                e.Value = label;
            }
        }

        bool IsAddressPointingToCode(int address)
            => (address - Cpu.RY is var row &&
                row >= 0 && row < FrameItems.Length && FrameItems[row]?.Cat == "M") ||
                Compilation.AddressLabels.TryGetValue(address, out var label) && label.StartsWith("M:");

        private void Memory_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var address = (int)((DataGridView)sender).Tag + e.RowIndex;

            if (IsAddressPointingToCode(address))
            {
                SelectAddrIdx(Config.AddrSlotToAddrIdx(Cpu.Memory[address]));
            }
            else if (e.ColumnIndex == MemoryValue.Index)
            {
                SelectAddress(Cpu.Memory[address]);
            }
        }

        private void Memory_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || ((DataGridView)sender).Tag == null)
            {
                return;
            }

            var address = (int)((DataGridView)sender).Tag + e.RowIndex;

            if (e.ColumnIndex == MemoryValue.Index && Cpu.Memory[address] != LastCpu.Memory[address])
            {
                e.CellStyle.ForeColor = Color.Red;
            }

            if (sender == Frame && IsAddressPointingToCode(address))
            {
                e.CellStyle.BackColor = Color.Yellow;
            }
        }

        private void Code_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var codeline = CodeLines?[e.RowIndex];

            if (e.ColumnIndex == CodeBreakpoint.Index && codeline != null)
            {
                AddressesByAddrIdx[codeline.AddrIdx].BreakPoint = !AddressesByAddrIdx[codeline.AddrIdx].BreakPoint;
            }
        }

        private void Registers_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex == RegisterName.Index)
            {
                e.Value = RegisterValues[e.RowIndex].Name;
            }
            else if (e.ColumnIndex == RegisterValue.Index)
            {
                object value = RegisterValues[e.RowIndex].GetValue(Cpu);
                e.Value = DisplayNumber(value);
            }
        }

        private void Registers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == RegisterValue.Index)
            {
                object value = RegisterValues[e.RowIndex].GetValue(Cpu);
                object lastValue = RegisterValues[e.RowIndex].GetValue(LastCpu);

                if (DisplayNumber(value) != DisplayNumber(lastValue))
                {
                    e.CellStyle.ForeColor = Color.Red;
                }
            }
        }

        private void SelectAddrIdx(int addrIdx)
        {
            var coderow = AddressesByAddrIdx[addrIdx].Line?.Index ?? -1;

            if (coderow >= 0 && coderow < Code.RowCount)
            {
                Code.Rows[coderow].Selected = true;
                Code.CurrentCell = Code.Rows[coderow].Cells[0];
                Code.Focus();
            }
        }

        private void SelectAddress(int address)
        {
            var framerow = address - Cpu.RY;

            if (framerow >= 0 && framerow < Frame.RowCount)
            {
                TabControl.SelectedTab = FrameTabPage;
                Frame.Rows[framerow].Selected = true;
                Frame.CurrentCell = Frame.Rows[framerow].Cells[0];
                Frame.Focus();
            }
            else if (address >= 0 && address < Memory.RowCount)
            {
                TabControl.SelectedTab = MemoryTabPage;
                Memory.Rows[address].Selected = true;
                Memory.CurrentCell = Memory.Rows[address].Cells[0];
                Memory.Focus();
            }
        }

        private void Registers_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == RegisterValue.Index &&
                    (RegisterValues[e.RowIndex].Name == "PC" ||
                     RegisterValues[e.RowIndex].Name == "SLOT"))
            {
                SelectAddrIdx(Cpu.CurrentAddrIdx);
            }
            else if (e.ColumnIndex == RegisterValue.Index)
            {
                var value = RegisterValues[e.RowIndex].GetValue(Cpu);

                if (value is int integer)
                    SelectAddress(integer);
                else if (value is Cpu.RegisterValue register && !register.IsFloat)
                    SelectAddress(register.Int);
            }
        }

        private void Status_Click(object sender, EventArgs e)
        {
            if (Status.Tag != null)
            {
                MessageBox.Show($"{Status.Tag}");
            }
        }

        private void Detail_Click(object sender, EventArgs e)
        {
            PfxDetail.Checked = sender == PfxDetail;
            OpcodeDetail.Checked = sender == OpcodeDetail;
            AsmDetail.Checked = sender == AsmDetail;
            RefreshCode();
        }

        private void Base_Click(object sender, EventArgs e)
        {
            Dec.Checked = sender == Dec;
            Hex.Checked = sender == Hex;
            RefreshCode();
        }
    }

    public class BreakException : Exception
    {
    }

    public class RegisterValue
    {
        public string Name { get; set; }
        public Func<Cpu, object> ValueFunc { get; set; }
        public object GetValue(Cpu cpu)
        {
            try { return ValueFunc(cpu); }
            catch { return null; }
        }
    }

    public class CodeLine
    {
        public int AddrIdx { get; set; }
        public int Range { get; set; }
        public int Index { get; internal set; }
        public string Line { get; set; }
        public bool IsMethod { get; set; }
    }

    public struct AddressSlot
    {
        public bool BreakPoint { get; set; }
        public CodeLine Line { get; set; }
        public MethodData Method { get; set; }
    }
}
