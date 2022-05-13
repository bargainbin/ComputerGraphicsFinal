/* SerializationUtil
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System;
using System.IO;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Public interface of MiddleVR.Unity.Serialization
    ///
    /// Supported types:
    /// - Any basic types
    /// - Any blittable/unmanaged struct
    /// - Lists/Dictionary
    /// - Structs and Classes marked with the [Serializable] attributes:
    ///    * Includes any public field of a type that is serializable,
    ///    * Includes non-public fields if they have the [UnityEngine.SerializeField] attribute,
    ///    * Public fields with the [NonSerialized] attribute are skipped.
    ///
    /// Notably NOT Supported:
    /// - Arrays (please use List<T>/Dictionary<K,V> instead)
    ///
    /// Additionally, the top-level type being serialized can be a derived
    /// type of `UnityEngine.MonoBehaviour` or `UnityEngine.ScriptableObject`.
    /// 
    /// Limitations of this implementation:
    /// - It does not support arbitrary object graphs. Reference cyles will be ignored.
    /// - It uses dynamic code generation thus it is incompatible
    ///   with any AOT (Ahead Of Time) compilation mode like IL2CPP.
    ///
    /// Performance considerations:
    /// - Prefer avoiding polymorphic references in serialized objects as it is considerably slower
    ///   to serialize/deserialize.
    /// </summary>
    public static class SerializationUtil
    {
        private static readonly SerializationContext s_ctx = new SerializationContext();

        public static void Write<T>(BinaryWriter writer, ref T val)
        {
            if (!SerializationRegistry.IsTypeSerializable<T>())
                throw new ArgumentException($"{typeof(T)} must be serializable");

            var writeDelegate = SerializationRegistry<T>.GetWrite();
            if (writeDelegate == null)
                throw new InvalidOperationException($"Cannot serialize {typeof(T)}");

            s_ctx.Reset();

            writeDelegate(writer, ref val, s_ctx);
        }

        public static void Read<T>(BinaryReader reader, ref T val)
        {
            if (!SerializationRegistry.IsTypeSerializable<T>())
                throw new ArgumentException($"{typeof(T)} must be serializable");

            var readDelegate = SerializationRegistry<T>.GetRead();
            if (readDelegate == null)
                throw new InvalidOperationException($"Cannot serialize {typeof(T)}");

            s_ctx.Reset();

            readDelegate(reader, ref val, s_ctx);
        }
    }
}
#endif
