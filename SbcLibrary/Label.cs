using System;
using System.Diagnostics;

namespace SbcLibrary
{
    public class Label
    {
        public string Name { get; }
        public int Value { get; set; }
        public bool IsAddressSlot { get; set; }
        public bool RemoveCall { get; set; }
        public object Owner { get; set; }

        public Label(string name, object owner, int value = 0, bool isAddressSlot = false)
        {
            Debug.Assert(name != null);
            Name = name;
            Value = value;
            IsAddressSlot = isAddressSlot;
            Owner = owner;
        }

        public Label(Type type, object owner, int value = 0)
            : this(type.Id(), owner, value)
        {
        }

        public Label(Node node, object owner, int value = 0, bool isAddressSlot = false)
            : this(node.Id, owner, value, isAddressSlot)
        {
        }

    }
}