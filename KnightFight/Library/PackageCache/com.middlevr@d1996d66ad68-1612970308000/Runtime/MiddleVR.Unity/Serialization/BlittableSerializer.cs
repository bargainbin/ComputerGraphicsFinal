/* BlittableSerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
#if UNITY_2019_3_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialize unmanaged types by copying memory.
    /// 
    /// Unmanaged types include:
    /// - Booleans
    /// - Numeric types
    /// - Enums
    /// - Structs of unmanaged types
    /// </summary>
    public static class BlittableSerializer
    {
        private static readonly Dictionary<Type, bool> s_cachedTypes =
            new Dictionary<Type, bool>();

        private static class IsUnmanagedGenericCache<T>
        {
            public static readonly bool Value = IsUnmanaged(typeof(T));
        }

        public static bool IsUnmanaged<T>()
        {
            return IsUnmanagedGenericCache<T>.Value;
        }

        public static bool IsUnmanaged(Type t)
        {
            var result = false;
            if (s_cachedTypes.ContainsKey(t))
                return s_cachedTypes[t];
            else if (t.IsPrimitive || t.IsEnum)
                result = true;
            else if (t.IsPointer || t.IsGenericType || !t.IsValueType)
                result = false;
            else
#if UNITY_2019_3_OR_NEWER
                result = UnsafeUtility.IsUnmanaged(t);
#else
                result = t.GetFields(BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    .All(x => IsUnmanaged(x.FieldType));
#endif
            s_cachedTypes.Add(t, result);
            return result;
        }

        public static ISerializer<T> FromType<T>()
        {
            var type = typeof(T);

            // Check that the type matches the serializer constraints (T : unmanaged)
            if (!IsUnmanaged(type))
                return null;

            var serializerType = typeof(BlittableSerializer<>).MakeGenericType(type);
            return (ISerializer<T>)Activator.CreateInstance(serializerType);
        }
    }

    public class BlittableSerializer<T> : ISerializer<T>
        where T : unmanaged
    {
        private readonly byte[] _buffer;

        public BlittableSerializer()
        {
            unsafe
            {
                _buffer = new byte[sizeof(T)];
            }
        }

        public void Read([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx)
        {
            unsafe
            {
                reader.BaseStream.Read(_buffer, 0, sizeof(T));
                fixed (byte* bufPtr = _buffer)
                {
                    val = *(T*)bufPtr;
                }
            }
        }

        public void Write([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx)
        {
            unsafe
            {
                fixed (byte* bufPtr = _buffer)
                {
                    *(T*)bufPtr = val;
                }
                writer.BaseStream.Write(_buffer, 0, sizeof(T));
            }
        }
    }
}
#endif
