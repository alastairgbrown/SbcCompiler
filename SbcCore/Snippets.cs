using SbcLibrary;
using System;
using System.Linq;

namespace SbcCore
{
    public class Snippets : ISnippets
    {
        public Action GetRX => () => Global.Emit(Opcode.SWX, Opcode.DUP, Opcode.SWX, Opcode.POP);
        public Action GetRY => () => Global.Emit(Opcode.SWY, Opcode.DUP, Opcode.SWY, Opcode.POP);
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
                for (int i = 0; i < local.ElementSize; i++)
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

        public Action MFD => () =>
        {
            var exit = Global.Compiler.GetNewLabel();
            var loop = Global.Compiler.GetNewLabel();
            Global.Compiler.LabelDefs[loop] = new Label(loop, Global.Compiler.CurrentAddrIdx, true);
            Global.Emit(new Label(exit), Opcode.JPZ, Opcode.MFD, new Label(loop), Opcode.JMP);
            Global.Compiler.LabelDefs[exit] = new Label(exit, Global.Compiler.CurrentAddrIdx, true);
        };

        public Action MBD => () =>
        {
            var exit = Global.Compiler.GetNewLabel();
            var loop = Global.Compiler.GetNewLabel();
            Global.Compiler.LabelDefs[loop] = new Label(loop, Global.Compiler.CurrentAddrIdx, true);
            Global.Emit(new Label(exit), Opcode.JPZ, Opcode.MBD, new Label(loop), Opcode.JMP);
            Global.Compiler.LabelDefs[exit] = new Label(exit, Global.Compiler.CurrentAddrIdx, true);
        };

        public Action CopyFromFrameToStack(ArgData arg) => () =>
        {
            Global.Compiler.TypePush(arg.Type);
            for (int i = 0; i < arg.ElementSize; i++)
                Global.Emit(arg.Offset + i, Opcode.LDY, StackPush);
        };

        public Action CopyFromStackToFrame(ArgData arg) => () =>
        {
            Global.Compiler.TypePop();
            for (int i = arg.ElementSize - 1; i >= 0; i--)
                Global.Emit(StackPop, arg.Offset + i, Opcode.STY);
        };

        public Action CopyFromMemoryToStack(TypeData type) => () =>
        {
            var elementSize = Global.Compiler.GetElementSize(type);

            Global.Compiler.TypePush(type);
            if (elementSize == 1)
                Global.Emit(Opcode.LDA, StackPush);
            else
                Global.Emit(elementSize, Opcode.AKX,
                            GetRX, elementSize - 1, Opcode.AKA, Opcode.SWP, elementSize, Opcode.PSH,
                            MFD);
        };

        public Action CopyFromStackToMemory(TypeData type) => () =>
        {
            var elementSize = Global.Compiler.GetElementSize(type);
            Global.Compiler.TypePop();
            if (elementSize == 1)
                Global.Emit(StackPop, Opcode.SWP, Opcode.STA);
            else
                Global.Emit(GetRX, elementSize - 1, Opcode.AKA, elementSize, Opcode.PSH,
                            MFD,
                            -elementSize, Opcode.AKX);
        };
    }
}
