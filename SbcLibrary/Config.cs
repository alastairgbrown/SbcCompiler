using System;

namespace SbcLibrary
{
    public class Config
    {
        public int MemorySize { get; set; } = 0x40004;

        public int SlotsPerMemoryUnit { get; set; } = 5;

        public int BitsPerMemoryUnit { get; set; } = 32;

        public int SlotBits { get; set; } = 3;

        public int PfxBits { get; set; } = 4;

        public int EntryPoint { get; set; } = 0;

        public int ExecutableStart { get; set; } = 0x0;

        public int ExecutableSize { get; set; } = 0x0FFF0;

        public int HeapPointer { get; set; } = 0x0FFFF;

        public int StackStart { get; set; } = 0x10000;

        public int StackSize { get; set; } = 0x10000;

        public int HeapStart { get; set; } = 0x20000;

        public int HeapSize { get; set; } = 0x10000;

        public int HeapGranularity { get; set; } = 16;

        public int HeapMarkerStart { get; set; } = 0x30000;

        public int HeapMarkerSize { get; set; } = 0x1000;

        public int StaticStart { get; set; } = 0x31000;

        public int StaticSize { get; set; } = 0x0F000;

        public int OutputAddress { get; set; } = 0x40000;

        public int InputAddress { get; set; } = 0x40001;

        public int InputReadyAddress { get; set; } = 0x40002;

        public int BreakAddress { get; set; } = 0x40003;

        public int StepsPerRun { get; set; } = 1000000;

        public int MaxPfx() => (1 << PfxBits) - 1;
        public int AddrSlotToAddrIdx(int addr, int slot) => (addr * SlotsPerMemoryUnit) + slot;
        public int AddrSlotToAddrSlot(int addr, int slot) => (addr << SlotBits) + slot;
        public int AddrIdxToAddr(int addrIdx) => addrIdx / SlotsPerMemoryUnit;
        public int AddrIdxToSlot(int addrIdx) => addrIdx % SlotsPerMemoryUnit;
        public int AddrSlotToAddr(int addrIdx) => addrIdx >> SlotBits;
        public int AddrSlotToSlot(int addrIdx) => addrIdx & ((1 << SlotBits) - 1);
        public int AddrIdxToAddrSlot(int addrIdx) => AddrSlotToAddrSlot(AddrIdxToAddr(addrIdx), AddrIdxToSlot(addrIdx));
        public int AddrSlotToAddrIdx(int addrSlot) => AddrSlotToAddrIdx(AddrSlotToAddr(addrSlot), AddrSlotToSlot(addrSlot));
    }
}