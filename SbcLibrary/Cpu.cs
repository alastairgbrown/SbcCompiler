using SbcCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SbcLibrary
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Cpu
    {
        /// <summary>
        /// Program Counter
        /// </summary>
        [Register]
        public int PC { get; set; }

        /// <summary>
        /// Instruction Slot pointer(2 bits) (5 bit 00,01,10,11 6 bit 00,01,10/11)
        /// </summary>
        [Register]
        public int SLOT { get; set; }

        /// <summary>
        /// Top of stack and RA register
        /// </summary>
        [Register]
        public int RA { get; set; }

        /// <summary>
        /// Next in stack and RB register
        /// </summary>
        [Register]
        public int RB { get; set; }

        /// <summary>
        /// Lowest stack register
        /// </summary>
        [Register]
        public int RC { get; set; }

        /// <summary>
        /// Index X register
        /// </summary>
        [Register]
        public int RX { get; set; }

        /// <summary>
        /// Index Y register
        /// </summary>
        [Register]
        public int RY { get; set; }

        /// <summary>
        /// Prefix register
        /// </summary>
        [Register]
        public int RK { get; set; }

        /// <summary>
        /// IRQ return address register
        /// </summary>
        [Register]
        public int RI { get; set; }

        /// <summary>
        /// carry flag, boolean
        /// </summary>
        [Register]
        public int CF { get; set; }

        /// <summary>
        /// extend flag, boolean, indicated last opcode was EXT otherwise 0
        /// </summary>
        [Register]
        public int EF { get; set; }

        /// <summary>
        /// prefix flag, indicated last opcode was PFX otherwise 0
        /// </summary>
        [Register]
        public int PF { get; set; }

        /// <summary>
        /// IRQ flag, set when IRQ has been accepted and jumped to.cleared with IRQ opcode which also loads return address
        /// </summary>
        [Register]
        public int IRQF { get; set; }

        /// <summary>
        /// IRQ flag, set when IRQ has been accepted and jumped to.cleared with IRQ opcode which also loads return address
        /// </summary>
        [Register]
        public int InstructionStepCount { get; set; }

        public Config Config { get; set; }
        public int[] Memory { get; set; }
        public Dictionary<int, Action> Opcodes { get; }

        public Cpu()
        => Opcodes = GetType().GetMethods()
                    .SelectMany(m => m.GetCustomAttributes(false).OfType<OpcodeAttribute>()
                                      .Select(oc => new { oc.Opcode, Method = m }))
                    .ToDictionary(oc => (int)oc.Opcode, oc => (Action)oc.Method.CreateDelegate(typeof(Action), this));

        public static readonly Dictionary<string, PropertyInfo> RegisterProps = typeof(Cpu).GetProperties().Where(m => m.GetCustomAttributes(false).OfType<RegisterAttribute>().Any()).ToDictionary(r => r.Name);

        public int BitsPerSlot => Config.BitsPerMemoryUnit / Config.SlotsPerMemoryUnit;
        public int BitsPerMemoryUnit => Config.BitsPerMemoryUnit;
        public int PfxBits => Config.PfxBits;
        public int SlotsPerMemoryUnit => Config.SlotsPerMemoryUnit;
        public int CurrentOpcode => OpCodeAt(PC, SLOT);
        public int CurrentAddrSlot => Config.AddrSlotToAddrSlot(PC, SLOT);
        public int CurrentAddrIdx => Config.AddrSlotToAddrIdx(PC, SLOT);

        public int OpCodeAt(int addrIdx) => OpCodeAt(Config.AddrIdxToAddr(addrIdx), Config.AddrIdxToSlot(addrIdx));
        public int OpCodeAt(int addr, int slot) => (Memory[addr] >> (BitsPerSlot * slot)) & ((1 << BitsPerSlot) - 1);
        public event EventHandler<KeyValuePair<int, int>> OnSetMemory;

        public void Run(Func<bool> stop)
        {
            for (InstructionStepCount = 0; InstructionStepCount < Config.StepsPerRun; InstructionStepCount++)
            {
                if (!Opcodes.TryGetValue(CurrentOpcode, out var action))
                    throw new Exception($"Can't execute {(Opcode)CurrentOpcode} ({CurrentOpcode})");

                action();

                if (stop())
                {
                    return;
                }
            }

            throw new Exception($"Execution steps exceed {Config.StepsPerRun}");
        }

        public void SetMemory(int address, int value)
        {
            Memory[address] = value;

            OnSetMemory?.Invoke(this, new KeyValuePair<int, int>(address, value));
        }

        /// <summary>
        /// /****************************************************************************/
        /// PFX      0x00 ..0x0f       Prefix RK
        /// /----------------------------------------------------------------------------/ 
        /// RA' = unchanged
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = (pf == 1) ? ((RK &lt;&lt;&lt; 4) | ($inst &amp; $mask)) : $signed($inst &amp; $mask);
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 1
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.PFX0)]
        [Opcode(Opcode.PFX1)]
        [Opcode(Opcode.PFX2)]
        [Opcode(Opcode.PFX3)]
        [Opcode(Opcode.PFX4)]
        [Opcode(Opcode.PFX5)]
        [Opcode(Opcode.PFX6)]
        [Opcode(Opcode.PFX7)]
        [Opcode(Opcode.PFX8)]
        [Opcode(Opcode.PFX9)]
        [Opcode(Opcode.PFXA)]
        [Opcode(Opcode.PFXB)]
        [Opcode(Opcode.PFXC)]
        [Opcode(Opcode.PFXD)]
        [Opcode(Opcode.PFXE)]
        [Opcode(Opcode.PFXF)]
        public void PFX()
        {
            RK = ((PF == 1 ? RK : CurrentOpcode < (1 << (PfxBits - 1)) ? 0 : -1) << PfxBits) | CurrentOpcode;
            PF = 1;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// PSH      0x10              Push to stack
        /// /----------------------------------------------------------------------------/ 
        /// RA' = RK
        /// RB' = RA
        /// RC' = RB
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.PSH)]
        public void PSH()
        {
            RC = RB;
            RB = RA;
            RA = RK;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// LDX      0x11              Load RA from $mem(RX+RK)
        /// /----------------------------------------------------------------------------/ 
        /// RA' = $mem(RX + $signed(RK))
        /// RB' = RA
        /// RC' = RB
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.LDX)]
        public void LDX()
        {
            RC = RB;
            RB = RA;
            RA = Memory[RX + RK];
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        [Opcode(Opcode.LDY)]
        public void LDY()
        {
            RC = RB;
            RB = RA;
            RA = Memory[RY + RK];
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// STX      0x12              Store RA to $mem(RX+RK)
        /// /----------------------------------------------------------------------------/
        /// RA' = RB
        /// RB' = RC
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.STX)]
        public void STX()
        {
            SetMemory(RX + RK, RA);
            RA = RB;
            RB = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        [Opcode(Opcode.STY)]
        public void STY()
        {
            SetMemory(RY + RK, RA);
            RA = RB;
            RB = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// LDA      0x13              Load RA from $mem(RA+RK)
        /// /----------------------------------------------------------------------------/
        /// RA' = $mem(RA + $signed(RK))
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.LDA)]
        public void LDA()
        {
            RA = Memory[RA + RK];
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// STA      0x14              Store RB to $mem(RA+RK)
        /// /----------------------------------------------------------------------------/
        /// RA' = RC
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>       
        [Opcode(Opcode.STA)]
        public void STA()
        {
            SetMemory(RA + RK, RB);
            RA = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// SWP      0x15              Swap Stack
        /// /----------------------------------------------------------------------------/
        /// RA' = RB
        /// RB' = RA
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.SWP)]
        public void SWP()
        {
            var ra = RA;
            RA = RB;
            RB = ra;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// DUP      0x16              Duplicate Stack
        /// /----------------------------------------------------------------------------/
        /// RA' = unchanged
        /// RB' = RA
        /// RC' = RB
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.DUP)]
        public void DUP()
        {
            RC = RB;
            RB = RA;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }


        /// <summary>
        /// /****************************************************************************/
        /// SWX      0x17              Swap RA and RX
        /// /----------------------------------------------------------------------------/
        /// RA' = RX
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = RA
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.SWX)]
        public void SWX()
        {
            var ra = RA;
            RA = RX;
            RX = ra;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        [Opcode(Opcode.SWY)]
        public void SWY()
        {
            var ra = RA;
            RA = RY;
            RY = ra;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// POP      0x18              Pop stack item
        /// /----------------------------------------------------------------------------/
        /// RA' = RB
        /// RB' = RC
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.POP)]
        public void POP()
        {
            RA = RB;
            RB = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// AKA      0x19              Add RK to RA
        /// /----------------------------------------------------------------------------/
        /// RA' = RA + $signed(RK)
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.AKA)]
        public void AKA()
        {
            RA += RK;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// AKX      0x1A              Add RK to RX
        /// /----------------------------------------------------------------------------/
        /// RA' = unchanged
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = RX + $signed(RK)
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.AKX)]
        public void AKX()
        {
            RX += RK;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;

            if (RX < Config.StackStart || (RX > RY && RY > 0))
                throw new Exception("Stack underflow/overflow");
        }
        [Opcode(Opcode.AKY)]
        public void AKY()
        {
            RY += RK;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// JMP      0x1B              Jump to RK
        /// /----------------------------------------------------------------------------/
        /// RA' = unchanged
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = unchanged
        /// {PC', SLOT'} = RK
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.JMP)]
        public void JMP()
        {
            SLOT = Config.AddrSlotToSlot(RK);
            PC = Config.AddrSlotToAddr(RK);
            RK = PF = EF = 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// JPZ      0x1C              Jump if RA==0 to RK
        /// /----------------------------------------------------------------------------/
        /// RA' = (RA == 0) ? RB : RA
        /// RB' = (RA == 0) ? RC : RB
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// {PC', SLOT'} = (RA == 0) ? RK : $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// </summary>
        [Opcode(Opcode.JPZ)]
        public void JPZ()
        {
            if (RA == 0)
            {
                SLOT = Config.AddrSlotToSlot(RK);
                PC = Config.AddrSlotToAddr(RK);
                RA = RB;
                RB = RC;
            }
            else
            {
                PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
            }
            RK = PF = EF = 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// JSR      0x1D              Jump to subroutine
        /// /----------------------------------------------------------------------------/
        /// RA' = $next_address
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// {PC', SLOT'} = RA
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.JSR)]
        public void JSR()
        {
            var ra = RA;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
            RA = CurrentAddrSlot;
            RK = PF = EF = 0;
            PC = Config.AddrSlotToAddr(ra);
            SLOT = Config.AddrSlotToSlot(ra);
        }

        /// <summary>
        /// /****************************************************************************/
        /// NOP      0x1E              No-Operation
        /// /----------------------------------------------------------------------------/
        /// RA' = unchanged
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = unchanged
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = unchanged
        /// cf' = unchanged
        /// ef' = unchanged
        /// </summary>
        [Opcode(Opcode.NOP)]
        public void NOP()
        {
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// ALU Opcodes are below - all operate in one cycle (after EXT for ISA5)
        /// /****************************************************************************/
        /// 
        /// /****************************************************************************/
        /// ADD      0x00              Signed Add RB to RA with carry out
        /// /----------------------------------------------------------------------------/
        /// RA' = $signed(RB) + $signed(RA)
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = carry out
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.ADD)]
        public void ADD()
        {
            RA += RB;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// SUB      0x02              Signed Subtract RB from RA with carry out
        /// /----------------------------------------------------------------------------/
        /// RA' = $signed(RB) + $signed(~RA) + 1
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = carry out
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.SUB)]
        public void SUB()
        {
            RA = RB - RA;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// AND      0x03              Logical bitwise AND of RA and RB
        /// /----------------------------------------------------------------------------/
        /// RA' = RA &amp; RB
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.AND)]
        public void AND()
        {
            RA = RB & RA;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// IOR      0x04              Logical bitwise OR of RA and RB
        /// /----------------------------------------------------------------------------/
        /// RA' = RA | RB
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.IOR)]
        public void IOR()
        {
            RA = RB | RA;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// XOR      0x05              Logical bitwise eclusive or of RA and RB
        /// /----------------------------------------------------------------------------/
        /// RA' = RA ^ RB
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.XOR)]
        public void XOR()
        {
            RA = RB ^ RA;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// MLT      0x06              Full signed multply of RB and RA
        /// /----------------------------------------------------------------------------/
        /// RA' = $msb($signed(RB) * $signed(RA)) 
        /// RB' = $lsb($signed(RB) * $signed(RA))
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.MLT)]
        public void MLT()
        {
            var product = (long)RA * RB;
            RA = (int)(product >> BitsPerMemoryUnit);
            RB = (int)(product & ((1L << BitsPerMemoryUnit) - 1));
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// SHR 0x38 Full barrel shift right RA >> RB
        /// /----------------------------------------------------------------------------/
        /// RA' = RA >> RB
        /// RB' = RC
        /// RC' = unchanged
        /// RX' = unchanged
        /// RY' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// irqf' = unchanged
        /// </summary>
        [Opcode(Opcode.SHR)]
        public void SHR()
        {
            RA = RA >> RB;
            RB = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// SHL 0x39 Full barrel shift left RA << RB
        /// /----------------------------------------------------------------------------/
        /// RA' = RA << RB
        /// RB' = RC
        /// RC' = unchanged
        /// RX' = unchanged
        /// RY' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// irqf' = unchanged
        /// </summary>
        [Opcode(Opcode.SHL)]
        public void SHL()
        {
            RA = RA << RB;
            RB = RC;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// ZEQ      0x08              Test for RA == 0
        /// /----------------------------------------------------------------------------/
        /// RA' = (RA == 0) ? 1 : 0
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.ZEQ)]
        public void ZEQ()
        {
            RA = RA == 0 ? 1 : 0;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// AGB      0x09              Test for RA &gt; RB
        /// /----------------------------------------------------------------------------/
        /// RA' = (RA &gt; RB) ? 1 : 0
        /// RB' = unchanged
        /// RC' = unchanged
        /// RX' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.AGB)]
        public void AGB()
        {
            RA = RA > RB ? 1 : 0;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// MFD 0x2B Move forward data from $mem(RC++) = $mem(RB++); RA--;
        /// /----------------------------------------------------------------------------/
        /// RA' = RA--
        /// RB' = RB++
        /// RC' = RC++
        /// RX' = unchanged
        /// RY' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// irqf' = unchanged
        /// </summary>
        [Opcode(Opcode.MFD)]
        public void MFD()
        {
            Memory[RC++] = Memory[RB++];
            RA--;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// MBD 0x2C Move backward data from $mem(RC--) = $mem(RB--); RA--;
        /// /----------------------------------------------------------------------------/
        /// RA' = RA--
        /// RB' = RB--
        /// RC' = RC--
        /// RX' = unchanged
        /// RY' = unchanged
        /// RK' = 0
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// irqf' = unchanged
        /// </summary>
        [Opcode(Opcode.MBD)]
        public void MBD()
        {
            Memory[RC--] = Memory[RB--];
            RA--;
            RK = PF = EF = 0;
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        /// <summary>
        /// /****************************************************************************/
        /// IRQ      0x0A              push IRQ return address
        /// /----------------------------------------------------------------------------/
        /// RA' = RI
        /// RB' = RA
        /// RC' = RB
        /// RX' = unchanged
        /// RK' = 0
        /// RI' = unchanged
        /// PC' = $next_address
        /// SLOT' = $next_address
        /// pf' = 0
        /// cf' = unchanged
        /// ef' = 0
        /// sf' = 0
        /// </summary>
        [Opcode(Opcode.IRQ)]
        public void IRQ()
        {
            PC += (SLOT = (SLOT + 1) % SlotsPerMemoryUnit) == 0 ? 1 : 0;
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        private class OpcodeAttribute : Attribute
        {
            public Opcode Opcode { get; }

            public OpcodeAttribute(Opcode opcode)
            {
                Opcode = opcode;
            }
        }

        private class RegisterAttribute : Attribute
        {
        }
    }
}