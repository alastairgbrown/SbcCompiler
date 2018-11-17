using SbcLibrary;
using System;
using System.Collections.Generic;

namespace SbcCore
{
    [ImplementClass(typeof(Array))]
    class System_Array
    {
        public static void Clear(Array array, int index, int length)
            => Memory.Clear(array.As<int>() + 1 + index, length);

        public static void Copy(Array src, int srcIdx, Array dst, int dstIdx, int len)
            => Copy((object)src, srcIdx, dst, dstIdx, len);

        public static void Copy(object src, int srcIdx, Array dst, int dstIdx, int len)
        {
            if (srcIdx > dstIdx)
                Memory.MFD(len, src.As<int>() + 1 + srcIdx, dst.As<int>() + 1 + dstIdx);
            else
                Memory.MBD(len, src.As<int>() + len + srcIdx, dst.As<int>() + len + dstIdx);
        }

        public static int BinarySearch<T>(T[] array, T value, IComparer<T> comparer)
            => BinarySearch(array, 0, array.Length, value, comparer);

        // Searches a section of an array for a given element using a binary search
        // algorithm. Elements of the array are compared to the search value using
        // the given IComparer interface. If comparer is null,
        // elements of the array are compared to the search value using the
        // IComparable interface, which in that case must be implemented by
        // all elements of the array and the given search value. This method
        // assumes that the array is already sorted; if this is not the case, the
        // result will be incorrect.
        // 
        // The method returns the index of the given value in the array. If the
        // array does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value.
        public static int BinarySearch<T>(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                // i might overflow if lo and hi are both large positive numbers. 
                int i = lo + ((hi - lo) >> 1);
                int c = comparer.Compare(array[i], value);

                if (c == 0)
                    return i;

                if (c < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return ~lo;
        }
    }
}
