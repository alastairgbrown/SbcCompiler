using SbcLibrary;
using System;
using System.Diagnostics;

namespace SbcCore
{
    [ImplementClass(typeof(GC))]
    public class System_GC
    {
        public static void Collect()
        {
            var stack = Memory.RX;
            var frame = Memory.RY + Memory.CurrentFrameSize;

            MarkValidBlocks();

            // Recursively mark all the claimed blocks with 2
            CollectRange(Global.Config.StackStart, stack - 1);
            CollectRange(frame, Global.Config.StackStart + Global.Config.StackSize);
            CollectRange(Global.Config.StaticStart, Global.Config.StaticStart + Memory.StaticSize);

            ReclaimBlocks();
            JoinContiguousFreeBlocks();
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


        private static void CollectRange(int start, int stop)
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
                        CollectRange(addr + 2, addr + 2 + length);
                    }
                }
            }
        }

        private static void MarkValidBlocks()
        {
            Memory.Clear(Global.Config.HeapMarkerStart, Global.Config.HeapMarkerSize);

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

        private static void ReclaimBlocks()
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
                    Memory.Clear(addr, -size);
                    Global.Memory[addr] = size;
                }
            }
        }

        private static void JoinContiguousFreeBlocks()
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

    }
}
