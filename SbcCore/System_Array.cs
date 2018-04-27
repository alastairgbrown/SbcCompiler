
using SbcLibrary;

namespace SbcCore
{
    class System_Array
    {
        [Implement("void System.Array::Copy(System.Array src,int32 srcIdx,System.Array dst,int32 dstIdx,int32 len)")]
        public static void Copy(object[] src, int srcIdx, object[] dst, int dstIdx, int len)
        {
            if (srcIdx > dstIdx)
            {
                for (int i = 0; i < len; i++)
                {
                    dst[dstIdx + i] = src[srcIdx + i];
                }
            }
            else 
            {
                for (int i = len - 1; i >= 0; i--)
                {
                    dst[dstIdx + i] = src[srcIdx + i];
                }
            }
        }
    }
}
