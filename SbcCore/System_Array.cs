
using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Array")]
    class System_Array
    {
        public static void Clear(System.Array array, int index, int length)
            => System_GC.Clear(ArrayAddress(array, index), length);

        public static void Copy(System.Array src, int srcIdx, System.Array dst, int dstIdx, int len)
            => Copy((object)src, srcIdx, dst, dstIdx, len);

        public static void Copy(object src, int srcIdx, System.Array dst, int dstIdx, int len)
        {
            if (len == 0)
                return;

            if (srcIdx > dstIdx)
                System_GC.MFD(len, ArrayAddress(src, srcIdx), ArrayAddress(dst, dstIdx));
            else
                System_GC.MBD(len, ArrayAddress(src, srcIdx + len - 1), ArrayAddress(dst, dstIdx + len - 1));
        }

        [Inline]
        public static int ArrayAddress(object obj, int index)
            => Global.Emit<int>(Global.Snippets.StackPop, 
                                Global.Snippets.StackGet, Opcode.ADD, 1, Opcode.AKA, 
                                Global.Snippets.StackSet);
    }
}
