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
            var args = Global.Compiler.CurrentArgs;

            // Save the return address and move the frame pointer
            Global.Emit(-Global.Compiler.CurrentFrameSize, Opcode.AKY);
            Global.Emit(Global.Compiler.CurrentFrameSize, Opcode.STY);

            // copy stack to args
            for (int i = 0; i < args.Length; i++)
            {
                Global.Emit(-args.Length + 1 + i, Opcode.LDX, args[i].Offset, Opcode.STY);
            }

            // Consume the stack
            if (args.Any())
            {
                Global.Emit(-Global.Compiler.CurrentArgs.Length, Opcode.AKX);
            }

            // Blank out the variables
            foreach (var local in Global.Compiler.CurrentLocals)
            {
                for (int i = 0; i < local.Elements.Count; i++)
                    Global.Emit(Opcode.PSH, local.Offset + i, Opcode.STY);
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
            Global.Compiler.TypePush(arg.Type);
            for (int i = 0; i < arg.Elements.Count; i++)
                Global.Emit(arg.Offset + i, Opcode.LDY, StackPush);
        };

        public Action CopyFromStackToFrame(ArgData arg) => () =>
        {
            Global.Compiler.TypePop();
            for (int i = arg.Elements.Count - 1; i >= 0; i--)
                Global.Emit(StackPop, arg.Offset + i, Opcode.STY);
        };

        public Action CopyFromMemoryToStack(Type type) => () =>
        {
            var elementSize = Global.Compiler.GetElements(type).Count;

            Global.Compiler.TypePush(type);
            if (elementSize == 1)
                Global.Emit(Opcode.LDA, StackPush);
            else
                Global.Emit(elementSize, Opcode.AKX,
                            Opcode.PUX, 1 - elementSize, Opcode.AKA, Opcode.SWP, elementSize, Opcode.PSH,
                            Opcode.MFD);
        };

        public Action CopyFromStackToMemory(Type type) => () =>
        {
            var elementSize = Global.Compiler.GetElements(type).Count;

            Global.Compiler.TypePop();
            if (elementSize == 1)
                Global.Emit(StackPop, Opcode.SWP, Opcode.STA);
            else
                Global.Emit(Opcode.PUX, 1 - elementSize, Opcode.AKA, elementSize, Opcode.PSH,
                            Opcode.MFD,
                            -elementSize, Opcode.AKX);
        };

        public Action InsertStack(int index, int count) => () =>
        {
            Global.Emit(count, Opcode.AKX);

            if (index > 0)
                Global.Emit(Opcode.PUX, Opcode.PUX, -count, Opcode.AKA, index, Opcode.PSH, Opcode.MBD);
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
