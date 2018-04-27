using SbcLibrary;

namespace SbcCore
{
    public class Snippets : ISnippets
    {
        public Snippet GetY => new Snippet((compiler, config) => compiler.Emit(Opcode.SWY, Opcode.DUP, Opcode.SWY, Opcode.POP));
        public Snippet Ldlen => new Snippet((compiler, config) => compiler.Emit(StackPop, Opcode.LDA, StackPush));
        public Snippet StackPush => new Snippet((compiler, config) => compiler.Emit(1, Opcode.AKX, Opcode.STX));
        public Snippet StackPop => new Snippet((compiler, config) => compiler.Emit(Opcode.LDX, -1, Opcode.AKX));
        public Snippet StackPop2 => new Snippet((compiler, config) => compiler.Emit(Opcode.LDX, -1, Opcode.LDX, -2, Opcode.AKX));
        public Snippet StackDup => new Snippet((compiler, config) => compiler.Emit(Opcode.LDX, 1, Opcode.AKX, Opcode.STX));
        public Snippet StackDrop => new Snippet((compiler, config) => compiler.Emit(-1, Opcode.AKX));
        public Snippet StackGet => new Snippet((compiler, config) => compiler.Emit(Opcode.LDX));
        public Snippet StackSet => new Snippet((compiler, config) => compiler.Emit(Opcode.STX));

        public Snippet MethodPreamble => new Snippet((compiler, config) =>
        {
            int pos = compiler.CurrentArgIndex;

            // Save the return address and move the frame pointer
            compiler.Emit(Opcode.STY);
            compiler.Emit(-compiler.CurrentFrameSize, Opcode.AKY);

            if (compiler.CurrentCtor == 1)
            {
                // copy top of stack to arg 0 aka 'this'
                compiler.Emit(Opcode.LDX, pos++, Opcode.STY);

                // copy stack to args
                for (int i = 0; i < compiler.CurrentArgCount - 1; i++)
                {
                    compiler.Emit(-compiler.CurrentArgCount + 1 + i, Opcode.LDX, pos++, Opcode.STY);
                }
            }
            else
            {
                // copy stack to args
                for (int i = 0; i < compiler.CurrentArgCount; i++)
                {
                    compiler.Emit(-compiler.CurrentArgCount + 1 + i, Opcode.LDX, pos++, Opcode.STY);
                }
            }

            // Consume the stack
            if (compiler.CurrentArgCount > 0)
            {
                compiler.Emit(-compiler.CurrentArgCount, Opcode.AKX);
            }

            // Blank out the variables
            for (int i = 0; i < compiler.CurrentLocalCount; i++)
            {
                compiler.Emit(Opcode.PSH, pos++, Opcode.STY);
            }
        });

        public Snippet MethodReturn => new Snippet((compiler, config) =>
        {
            // Restore the frame pointer
            compiler.Emit(compiler.CurrentFrameSize, Opcode.AKY);

            // return
            compiler.Emit(Opcode.LDY, Opcode.JSR);
        });
    }
}
