using SbcLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SbcCore
{
    [ImplementClass("System.Collections.Generic.List")]
    public class System_Collections_Generic_List<T> : IEnumerable, IEnumerable<T>
    {
        private T[] _items = new T[Global.Config.HeapGranularity - 2];
        private int _size;

        public System_Collections_Generic_List()
        => _items = new T[Global.Config.HeapGranularity - 2];

        public System_Collections_Generic_List(int capacity)
        =>  _items = new T[capacity];

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                var olditems = _items;

                _items = new T[_items.Length * 2 < min ? min : _items.Length * 2];
                Array.Copy(olditems, 0, _items, 0, olditems.Length);
            }
        }

        public int Count => _size;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public void Add(T item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size++] = item;
        }

        public void Insert(int index, T item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
        }

        public void RemoveAt(int index)
        {
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                int i = _size;
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
            }
        }

        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private System_Collections_Generic_List<T> _list;
            private int _index;
            private T _current;

            internal Enumerator(System_Collections_Generic_List<T> list)
            {
                _list = list;
                _index = 0;
                _current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index < _list._size)
                {
                    _current = _list._items[_index];
                    _index++;
                    return true;
                }
                return false;
            }

            public T Current => _current;

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    Debug.Assert(_index > 0 && _index < _list._size + 1);
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                _index = 0;
                _current = default(T);
            }
        }
    }
}
