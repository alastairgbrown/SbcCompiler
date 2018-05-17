using SbcLibrary;
using System;
using System.Diagnostics;

namespace SbcCore
{
    [ImplementClass("System.GC")]
    public class System_GC
    {
        public static void Collect()
        {
            var stack = GetRX();
            var frame = GetRY() + GetFrameSize();

            GcMarkValidBlocks();

            // Recursively mark all the claimed blocks with 2
            GcCollectRange(Global.Config.StackStart, stack - 1);
            GcCollectRange(frame, Global.Config.StackStart + Global.Config.StackSize);
            GcCollectRange(Global.Config.StaticStart, Global.Config.StaticStart + GetConfigStaticSize());

            GcReclaimBlocks();
            GcJoinContiguousFreeBlocks();
            Global.Memory[Global.Config.HeapPointer] = Global.Config.HeapStart;
        }

        public int GetTotalMemory(bool forceFullCollection)
        {
            if (forceFullCollection)
                Collect();

            var total = 0;

            for (int addr = Global.Config.HeapStart, size;
                     addr < Global.Config.HeapStart + Global.Config.HeapSize;
                     addr += size < 0 ? -size : ((size + 1) | (Global.Config.HeapGranularity - 1)) + 1)
            {
                size = Global.Memory[addr];

                total += size < 0 ? 0 : size;
            }

            return total;
        }

        public static int Newobj(int fieldcount, int vtable)
        {
            var pos = New(fieldcount);

            Global.Memory[pos - 1] = vtable;

            return pos;
        }

        public static int Newarr(int elements, int vtable, int elementSize)
        {
            var pos = New(elements * elementSize);

            Global.Memory[pos - 1] = vtable;
            Global.Memory[pos] = elements;

            return pos;
        }

        public static int New(int elements)
        {
            var spaceRequired = ((elements + 1) | (Global.Config.HeapGranularity - 1)) + 1;

            for (int pass = 1, addr = Global.Memory[Global.Config.HeapPointer];
                     pass <= 2;
                     pass++, addr = Global.Config.HeapStart)
            {
                for (int size;
                         addr < Global.Config.HeapStart + Global.Config.HeapSize;
                         addr += size < 0 ? -size : size)
                {
                    size = Global.Memory[addr];

                    Debug.Assert(size > 0 || size < 0);
                    Debug.Assert(((size < 0 ? -size : size) & 0xF) == 0);

                    if (size < 0 && -size >= spaceRequired)
                    {
                        size = -size;
                        if (size > spaceRequired)
                        {
                            Global.Memory[addr + spaceRequired] = -(size - spaceRequired);
                        }

                        Global.Memory[Global.Config.HeapPointer] = addr + spaceRequired;
                        Global.Memory[addr] = spaceRequired;
                        Clear(addr + 2, spaceRequired - 2);
                        return addr + 2;
                    }
                }
            }

            Debug.Assert(false, "Out of memory");
            return 0;
        }

        public static void HeapInitialise()
        {
            Global.Memory[Global.Config.HeapPointer] = Global.Config.HeapStart;
            Global.Memory[Global.Config.HeapStart] = -Global.Config.HeapSize;
        }

        public static void Clear(int address, int length)
        {
            if (length > 0)
                Global.Memory[address] = 0;

            if (length - 1 > 0)
                MFD(length - 1, address, address + 1);
        }

        private static void GcCollectRange(int start, int stop)
        {
            for (int i = start; i < stop; i++)
            {
                var addr = Global.Memory[i];

                if ((addr & (Global.Config.HeapGranularity - 1)) == 2 &&
                    addr >= Global.Config.HeapStart &&
                    addr < Global.Config.HeapStart + Global.Config.HeapSize)
                {
                    var flagAddr = Global.Config.HeapMarkerStart + ((addr - Global.Config.HeapStart) >> 4);

                    if (Global.Memory[flagAddr] == 1)
                    {
                        Global.Memory[flagAddr] = 2;
                        var length = Global.Memory[addr];
                        GcCollectRange(addr + 2, addr + 2 + length);
                    }
                }
            }
        }

        private static void GcMarkValidBlocks()
        {
            Clear(Global.Config.HeapMarkerStart, Global.Config.HeapMarkerSize);

            // Mark all the allocated blocks with 1
            for (int addr = Global.Config.HeapStart, size;
                     addr < Global.Config.HeapStart + Global.Config.HeapSize;
                     addr += size < 0 ? -size : size)
            {
                size = Global.Memory[addr];

                if (size > 0)
                {
                    Global.Memory[Global.Config.HeapMarkerStart + ((addr - Global.Config.HeapStart) >> 4)] = 1;
                }
            }
        }

        private static void GcReclaimBlocks()
        {
            // Reclaim all the unclaimed memory (still marked with 1)
            for (int addr = Global.Config.HeapStart, size;
                     addr < Global.Config.HeapStart + Global.Config.HeapSize;
                     addr += size < 0 ? -size : size)
            {
                size = Global.Memory[addr];

                if (size > 0 &&
                    Global.Memory[Global.Config.HeapMarkerStart + ((addr - Global.Config.HeapStart) >> 4)] == 1)
                {
                    size = -size;
                    Clear(addr, -size);
                    Global.Memory[addr] = size;
                }
            }
        }

        private static void GcJoinContiguousFreeBlocks()
        {
            int lastfree = -1, lastsize = 0;

            for (int addr = Global.Config.HeapStart, size;
                     addr < Global.Config.HeapStart + Global.Config.HeapSize;
                     addr += size < 0 ? -size : size)
            {
                size = Global.Memory[addr];

                if (size > 0)
                {
                    lastfree = -1;
                }
                else if (lastfree < 0)
                {
                    lastfree = addr;
                    lastsize = size;
                }
                else
                {
                    lastsize += size;
                    Global.Memory[lastfree] = lastsize;
                    Global.Memory[addr] = 0;
                }
            }
        }

        [Inline]
        public static int GetFrameSize()
            => Global.Emit<int>(Global.Compiler.CurrentFrameSize, Opcode.PSH, Global.Snippets.StackPush);

        [Inline]
        public static int GetConfigStaticSize()
            => Global.Emit<int>(new Label("Config.StaticSize"), Opcode.PSH, Global.Snippets.StackPush);

        [Inline]
        public static int GetRX()
            => Global.Emit<int>(Global.Snippets.GetRX, Global.Snippets.StackPush);

        [Inline]
        public static int GetRY()
            => Global.Emit<int>(Global.Snippets.GetRY, Global.Snippets.StackPush);

        [Inline]
        public static void MFD(int ra, int rb, int rc)
            => Global.Emit(Global.Snippets.StackPop3, Global.Snippets.MFD);

        [Inline]
        public static void MBD(int ra, int rb, int rc)
            => Global.Emit(Global.Snippets.StackPop3, Global.Snippets.MBD);

        [Inline("void SbcLibrary.Memory::set_Item(int32,int32)")]
        public static void SetGlobalMemory()
            => Global.Emit(Global.Snippets.StackPop2, Opcode.STA);

        [Inline("int32 SbcLibrary.Memory::get_Item(int32)")]
        public static void GetGlobalMemory()
            => Global.Emit(Global.Snippets.StackGet, Opcode.LDA, Global.Snippets.StackSet);
    }
}
