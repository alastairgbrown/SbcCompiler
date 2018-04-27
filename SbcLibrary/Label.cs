using System.Diagnostics;

namespace SbcLibrary
{
    public class Label
    {
        public string Name { get; }
        public int Value { get; set; }
        public bool IsAddressSlot { get; set; }
        public bool RemoveCall { get; set; }

        public Label(string name, int value = 0, bool isAddressSlot = false)
        {
            Debug.Assert(name != null);
            Name = name;
            Value = value;
            IsAddressSlot = isAddressSlot;
        }

    }
}