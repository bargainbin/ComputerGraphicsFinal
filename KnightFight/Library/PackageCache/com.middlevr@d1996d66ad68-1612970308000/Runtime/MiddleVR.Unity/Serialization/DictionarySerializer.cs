/* DictionarySerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialize a Dictionary where keys and values are serializable
    /// </summary>
    /// <typeparam name="K">Key Type</typeparam>
    /// <typeparam name="V">Value Type</typeparam>
    class DictionarySerializer<K, V> : ISerializer<Dictionary<K, V>>
    {
        public void Read([NotNull] BinaryReader reader, ref Dictionary<K, V> dict, [NotNull] SerializationContext ctx)
        {
            var readKey = SerializationRegistry<K>.GetRead();
            var readValue = SerializationRegistry<V>.GetRead();

            if (readKey == null || readValue == null)
                return;

            dict.Clear();

            var count = reader.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                K key = default;
                readKey(reader, ref key, ctx);
                V value = default;
                readValue(reader, ref value, ctx);
                dict.Add(key, value);
            }
        }

        public void Write([NotNull] BinaryWriter writer, ref Dictionary<K, V> dict, [NotNull] SerializationContext ctx)
        {
            var writeKey = SerializationRegistry<K>.GetWrite();
            var writeValue = SerializationRegistry<V>.GetWrite();

            if (writeKey == null || writeValue == null)
                return;

            writer.Write(dict.Count);
            foreach (var kvp in dict)
            {
                var key = kvp.Key;
                writeKey(writer, ref key, ctx);
                var value = kvp.Value;
                writeValue(writer, ref value, ctx);
            }
        }
    }
}
#endif
