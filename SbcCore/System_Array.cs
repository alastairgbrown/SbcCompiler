using SbcLibrary;
using System;

namespace SbcCore
{
    [ImplementClass(typeof(Array))]
    class System_Array
    {
        public static void Clear(Array array, int index, int length)
            => SbcLibrary_Memory.Clear(array.AsAddress() + 1 + index, length);

        public static void Copy(Array src, int srcIdx, Array dst, int dstIdx, int len)
            => Copy((object)src, srcIdx, dst, dstIdx, len);

        public static void Copy(object src, int srcIdx, Array dst, int dstIdx, int len)
        {
            if (srcIdx > dstIdx)
                SbcLibrary_Memory.MFD(len, src.AsAddress() + 1 + srcIdx, dst.AsAddress() + 1 + dstIdx);
            else
                SbcLibrary_Memory.MBD(len, src.AsAddress() + len + srcIdx, dst.AsAddress() + len + dstIdx);
        }

    }
}
