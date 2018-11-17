using SbcLibrary;
using System;
using System.Diagnostics;
using System.Linq;

namespace SbcCore
{
    public class Snippets : ISnippets
    {
        public Action Ldlen => () => Global.Emit(StackGet, Opcode.LDA, StackSet);
        public Action StackPush => () => Global.Emit(1, Opcode.AKX, Opcode.STX);
        public Action StackPop => () => Global.Emit(Opcode.LDX, -1, Opcode.AKX);
        public Action StackPop2 => () => Global.Emit(Opcode.LDX, -1, Opcode.LDX, -2, Opcode.AKX);
        public Action StackPop3 => () => Global.Emit(Opcode.LDX, -1, Opcode.LDX, -2, Opcode.LDX, -3, Opcode.AKX);
        public Action StackDup => () => Global.Emit(Opcode.LDX, 1, Opcode.AKX, Opcode.STX);
        public Action StackDrop => () => Global.Emit(-1, Opcode.AKX);
        public Action StackGet => () => Global.Emit(Opcode.LDX);
        public Action StackSet => () => Global.Emit(Opcode.STX);

        public Action MethodPreamble => () =>
        {
            // Save the return address and move the frame pointer
            Global.Emit(Opcode.STY);
            Global.Emit(-Global.Compiler.CurrentFrameSize, Opcode.AKY);

            var argsSize = Global.Compiler.CurrentArgs.Sum(a => a.Elements.Count);
            var varsSize = Global.Compiler.CurrentLocals.Sum(a => a.Elements.Count);

            // Copy stack to args
            if (argsSize > 0)
            {
                if (argsSize == 1)
                    Global.Emit(StackGet, 1, Opcode.STY);
                else
                    Global.Emit(1, Opcode.PUY,            // Destination
                                1 - argsSize, Opcode.PUX, // Source
                                argsSize, Opcode.PSH,     // Size
                                Opcode.MFD);

                Global.Emit(-argsSize, Opcode.AKX);
            }

            // Clear vars
            if (varsSize > 0)
            {
                Global.Emit(Opcode.PSH, 1 + argsSize, Opcode.STY);

                if (varsSize > 1)
                    Global.Emit(2 + argsSize, Opcode.PUY, // Destination
                                1 + argsSize, Opcode.PUY, // Source
                                varsSize - 1, Opcode.PSH, // Size
                                Opcode.MFD);
            }
        };

        public Action MethodReturn => () =>
        {
            // Restore the frame pointer
            Global.Emit(Global.Compiler.CurrentFrameSize, Opcode.AKY);

            // return
            Global.Emit(Opcode.LDY, Opcode.JSR);
        };

        public Action CopyFromFrameToStack(ArgData arg) => () =>
        {
            var elementSize = arg.Elements.Count;
            Global.Compiler.TypePush(arg.Type);

            if (elementSize == 1)
                Global.Emit(arg.Offset, Opcode.LDY, StackPush);
            else
                Global.Emit(elementSize, Opcode.AKX,
                            1 - elementSize, Opcode.PUX, // Destination
                            arg.Offset, Opcode.PUY,      // Source
                            elementSize, Opcode.PSH,     // Size
                            Opcode.MFD);
        };

        public Action CopyFromStackToFrame(ArgData arg) => () =>
        {
            var elementSize = arg.Elements.Count;
            Global.Compiler.TypePop();
            if (elementSize == 1)
                Global.Emit(StackPop, arg.Offset, Opcode.STY);
            else
                Global.Emit(Opcode.PUY, arg.Offset, Opcode.AKA,      // Destination
                            Opcode.PUX, 1 - elementSize, Opcode.AKA, // Source
                            elementSize, Opcode.PSH,                 // Size
                            Opcode.MFD,
                            -elementSize, Opcode.AKX);
        };

        public Action CopyFromMemoryToStack(Type type) => () =>
        {
            var elementSize = Global.Compiler.GetElements(type).Count;

            Global.Compiler.TypePush(type);
            if (elementSize == 1)
                Global.Emit(Opcode.LDA, StackPush);
            else
                Global.Emit(elementSize, Opcode.AKX,
                            1 - elementSize, Opcode.PUX, // Destination
                            Opcode.SWP,                  // Source already on stack 
                            elementSize, Opcode.PSH,     // Size   
                            Opcode.MFD);
        };

        public Action CopyFromStackToMemory(Type type) => () =>
        {
            var elementSize = Global.Compiler.GetElements(type).Count;

            Global.Compiler.TypePop();
            if (elementSize == 1)
                Global.Emit(StackPop, Opcode.SWP, Opcode.STA);
            else
                Global.Emit(                             // Destination already on stack
                            1 - elementSize, Opcode.PUX, // Source
                            elementSize, Opcode.PSH,     // Size
                            Opcode.MFD,
                            -elementSize, Opcode.AKX);
        };

        public Action InsertStack(int index, int count) => () =>
        {
            Global.Emit(count, Opcode.AKX);

            if (index > 0)
                Global.Emit(Opcode.PUX, -count, Opcode.PUX, index, Opcode.PSH, Opcode.MBD);
        };

        public Action Compare(string keyword) => () =>
        {
            var comp = (Comp)Enum.Parse(typeof(Comp), keyword.Split('.').First(), true);
            bool unsigned = keyword.Split('.').Last() == "un";

            if (Global.Compiler.TypesAreFloats)
            {
                switch (comp)
                {
                    case Comp.BLT:
                    case Comp.BLE:
                    case Comp.BGE:
                    case Comp.BGT:
                    case Comp.CLT:
                    case Comp.CGT:
                        Global.Emit(-1, Opcode.PSH, Opcode.I2F, Opcode.FPM, Opcode.FPA, Opcode.PSH);
                        break;
                }
            }
            else if (unsigned)
            {
                Global.Emit(int.MinValue, Opcode.AKA, Opcode.SWP);
                Global.Emit(int.MinValue, Opcode.AKA, Opcode.SWP);
            }

            switch (comp)
            {
                case Comp.BGE:
                case Comp.CLT:
                    Global.Emit(Opcode.SWP, Opcode.AGB);
                    break;

                case Comp.BGT:
                case Comp.CLE:
                    Global.Emit(Opcode.AGB, Opcode.ZEQ);
                    break;

                case Comp.BLT:
                case Comp.CGE:
                    Global.Emit(Opcode.SWP, Opcode.AGB, Opcode.ZEQ);
                    break;

                case Comp.BLE:
                case Comp.CGT:
                    Global.Emit(Opcode.AGB);
                    break;

                case Comp.BEQ:
                case Comp.CNE:
                    Global.Emit(Opcode.SUB);
                    break;

                case Comp.BNE:
                case Comp.CEQ:
                    Global.Emit(Opcode.SUB, Opcode.ZEQ);
                    break;
            }
        };
    }

    public enum Comp
    {
        CLT, CLE, CEQ, CGE, CGT, CNE,
        BLT, BLE, BEQ, BGE, BGT, BNE
    }
}
