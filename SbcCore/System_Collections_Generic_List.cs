using SbcLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcCore
{
    [Implement("System.Collections.Generic.List`1<class >")]
    public class System_Collections_Generic_List
    {
        private object[] _items = new object[Global.Config.HeapGranularity - 3];
        private int _size;

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                var olditems = _items;

                _items = new object[_items.Length * 2 < min ? min : _items.Length * 2];
                Array.Copy(olditems, 0, _items, 0, olditems.Length);
            }
        }

        [Implement("int System.Collections.Generic.List`1<class >::get_Count()")]
        int Get_Count() => _size;

        [Implement("!0 System.Collections.Generic.List`1<class >::get_Item(int32 index)")]
        object Get_Item(int index) => _items[index];

        [Implement("void System.Collections.Generic.List`1<class >::set_Item(int32 index,!0 item)")]
        void Set_Item(int index, object item) => _items[index] = item;

        [Implement("void System.Collections.Generic.List`1<class >::Add(!0 item)")]
        void Add(object item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size++] = item;
        }

        [Implement("void System.Collections.Generic.List`1<class >::Insert(int index, !0 item)")]
        public void Insert(int index, object item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
        }

        [Implement("void System.Collections.Generic.List`1<class >::RemoveAt(int index)")]
        public void RemoveAt(int index)
        {
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = null;
        }
    }
}
