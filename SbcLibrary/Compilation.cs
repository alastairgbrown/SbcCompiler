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
        public List<int> StaticData { get; } = new List<int>();
        public SortedSet<int> AddressWritable { get; } = new SortedSet<int>();
        public SortedDictionary<int, string> ExecutableLines { get; } = new SortedDictionary<int, string>();
        public List<MethodData> MethodData { get; } = new List<MethodData>();
        public SortedDictionary<int, string> AddressLabels { get; } = new SortedDictionary<int, string>();

        public int[] GetInitialMemory()
        {
            var memory = new int[Config.MemorySize];

            for (int i = 0; i < Opcodes.Count; i++)
            {
                memory[Config.ExecutableStart + i / Config.SlotsPerMemoryUnit] |= (int)Opcodes[i] << ((i % Config.SlotsPerMemoryUnit) * (Config.BitsPerMemoryUnit / Config.SlotsPerMemoryUnit));
            }

            for (int i = 0; i < StaticData.Count; i++)
            {
                memory[Config.StaticStart + i] = StaticData[i];
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
                "CONTENT "
            };

            foreach (int i in Enumerable.Range(Config.ExecutableStart, Config.ExecutableSize).Concat(
                              Enumerable.Range(Config.StaticStart, Config.StaticStart)))
            {
                lines.Add($"{i:X}: {memory[i]:X};");
            }

            lines.Add("END;");
            File.WriteAllLines(filename, lines);
        }
    }
}
