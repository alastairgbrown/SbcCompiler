using SbcLibrary;
using System;

namespace SbcCore
{
    [Noclass]
    public class Implementations
    {
        public static int Newobj(int fieldcount, int vtable)
        {
            var pos = New(1 + fieldcount);

            Global.Memory[pos] = vtable;

            return pos + 1;
        }

        public static int Newarr(int elements)
        {
            var pos = New(2 + elements);

            Global.Memory[pos] = 0; // TODO vtable
            Global.Memory[pos + 1] = elements;

            return pos + 1;
        }

        public static int New(int elements)
        {
            var spaceRequired = ((1 + elements) | (Global.Config.HeapGranularity - 1)) + 1;

            for (int pass = 1, pos = Global.Memory[Global.Config.HeapPointer]; pass <= 2; pass++, pos = Global.Config.HeapSize)
            {
                while (pos < Global.Config.HeapStart + Global.Config.HeapSize)
                {
                    if (Global.Memory[pos] > 0)
                    {
                        pos += Global.Memory[pos];
                        continue;
                    }

                    var space = -Global.Memory[pos];

                    if (space < spaceRequired)
                    {
                        pos += -Global.Memory[pos];
                        continue;
                    }

                    if (space > spaceRequired)
                    {
                        Global.Memory[pos + spaceRequired] = -(space - spaceRequired);
                    }

                    Global.Memory[Global.Config.HeapPointer] = pos + spaceRequired;
                    Global.Memory[pos] = spaceRequired;

                    for (int i = 1; i <= elements; i++)
                    {
                        Global.Memory[pos + i] = 0;
                    }

                    return pos + 1;
                }
            }

            return 0;
        }

        public static void HeapInitialise()
        {
            Global.Memory[Global.Config.HeapPointer] = Global.Config.HeapStart;
            Global.Memory[Global.Config.HeapStart] = -Global.Config.HeapSize;
        }

    }

    [Noclass]
    public static class Global
    {
        public static Memory Memory;
        public static Config Config;
    }

    [Noclass]
    public class Memory
    {
        public int this[int address] { get => 0; set { } }

        [Snippet("void SbcCore.Memory::set_Item(int32,int32)")]
        public static Snippet SetGlobalMemory => new Snippet((compiler, config)
             => compiler.Emit(compiler.Snippets.StackPop2, Opcode.STA));

        [Snippet("int32 SbcCore.Memory::get_Item(int32)")]
        public static Snippet GetGlobalMemory => new Snippet((compiler, config)
             => compiler.Emit(compiler.Snippets.StackGet, Opcode.LDA, compiler.Snippets.StackSet));
    }
}
