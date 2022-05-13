/* Utils
 * MiddleVR
 * (c) MiddleVR
 */

using System;

namespace MiddleVR.Unity.Utils
{
    public static class UnsafeUtils
    {
        public static int SizeOfUnmanaged<T>()
            where T : unmanaged
        {
            unsafe
            {
                return sizeof(T);
            }
        }

        public static void UnmanagedToBufferAligned<T>(IntPtr buffer, T val)
            where T : unmanaged
        {
            unsafe
            {
                *(T*)buffer = val;
            }
        }

        public static T UnmanagedFromBufferAligned<T>(IntPtr buffer)
            where T : unmanaged
        {
            unsafe
            {
                return *(T*)buffer;
            }
        }

        public static int EnumToInt32<TEnum>(TEnum val)
            where TEnum : unmanaged, Enum
        {
            unsafe
            {
                switch (sizeof(TEnum))
                {
                    case 1:
                        return *(sbyte*)&val;
                    case 2:
                        return *(short*)&val;
                    case 4:
                        return *(int*)&val;
                    default:
                        throw new ArgumentException("Unsupported underlying enum type!");
                }
            }
        }
    }
}
