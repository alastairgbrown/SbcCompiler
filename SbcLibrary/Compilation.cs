using SbcCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SbcLibrary
{
    public class Compilation
    {
        public Config Config { get; } = new Config();
        public List<Opcode> Opcodes { get; } = new List<Opcode>();
        public List<int> ConstData { get; } = new List<int>();
        public int StaticDataCount { get; set; }
        public SortedDictionary<int, string> ExecutableLines { get; } = new SortedDictionary<int, string>();
        public List<MethodData> MethodData { get; } = new List<MethodData>();
        public SortedSet<int> AddressWritable { get; } = new SortedSet<int>();
        public SortedDictionary<int, string> AddressLabels { get; } = new SortedDictionary<int, string>();

        public int[] GetInitialMemory()
        {
            var memory = new int[Config.MemorySize];

            for (int i = 0; i < Opcodes.Count; i++)
            {
                memory[Config.ExecutableStart + i / Config.SlotsPerMemoryUnit] |= (int)Opcodes[i] << ((i % Config.SlotsPerMemoryUnit) * (Config.BitsPerMemoryUnit / Config.SlotsPerMemoryUnit));
            }

            for (int i = 0; i < ConstData.Count; i++)
            {
                memory[Config.ConstStart + i] = ConstData[i];
            }

            return memory;
        }

        public void WriteMif(string filename)
        {
            var memory = GetInitialMemory();
            var lines = new List<string>
            {
                "DEPTH = 32; % Memory depth and width are required %",
                "            % DEPTH is the number of addresses %",
                "WIDTH = 32; % WIDTH is the number of bits of data per word %",
                "            % DEPTH and WIDTH should be entered as decimal numbers %",
                "ADDRESS_RADIX = HEX; % Address and value radixes are required %",
                "DATA_RADIX = HEX;    % Enter BIN, DEC, HEX, OCT, or UNS; unless %",
                "                     % otherwise specified, radixes = HEX %",
                "CONTENT"
            };

            foreach (int i in Enumerable.Range(Config.ExecutableStart, Config.ExecutableSize).Concat(
                              Enumerable.Range(Config.ConstStart, Config.ConstSize)))
            {
                lines.Add($"    {i:X6}: {memory[i]:X8};");
            }

            lines.Add("END;");
            File.WriteAllLines(filename, lines);
        }

        public void SetAddressInfo()
        {
            Enumerable.Range(Config.ExecutableStart, Config.ExecutableSize).ToList().ForEach(a => AddressLabels[a] = "Executable");
            Enumerable.Range(Config.StackSize, Config.StackSize).ToList().ForEach(a => AddressLabels[a] = "Stack");
            Enumerable.Range(Config.HeapStart, Config.HeapSize).ToList().ForEach(a => AddressLabels[a] = "Heap");
            Enumerable.Range(Config.HeapMarkerStart, Config.HeapMarkerSize).ToList().ForEach(a => AddressLabels[a] = "HeapMarker");
            AddressLabels[Config.HeapPointer] = nameof(Config.HeapPointer);
            AddressLabels[Config.OutputAddress] = nameof(Config.OutputAddress);
            AddressLabels[Config.InputAddress] = nameof(Config.InputAddress);
            AddressLabels[Config.InputReadyAddress] = nameof(Config.InputReadyAddress);
            AddressLabels[Config.BreakAddress] = nameof(Config.BreakAddress);
            AddressWritable.Add(Config.HeapPointer);
            AddressWritable.UnionWith(Enumerable.Range(Config.StackStart, Config.StackSize));
            AddressWritable.UnionWith(Enumerable.Range(Config.HeapStart, Config.HeapSize));
            AddressWritable.UnionWith(Enumerable.Range(Config.HeapMarkerStart, Config.HeapMarkerSize));
            AddressWritable.UnionWith(Enumerable.Range(Config.StaticStart, Config.StaticSize));
            AddressWritable.Remove(Config.StackStart);
        }

    }
}
