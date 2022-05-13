/* ListSerializer
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
    /// Serialize a List
    /// </summary>
    /// <typeparam name="T">Type of List Item</typeparam>
    public class ListSerializer<T> : ISerializer<List<T>>
    {
        public void Read([NotNull] BinaryReader reader, ref List<T> list, [NotNull] SerializationContext ctx)
        {
            var readDelegate = SerializationRegistry<T>.GetRead();
            if (readDelegate == null)
                return;

            list.Clear();

            var count = reader.ReadInt32();

            if (list.Capacity < count)
            {
                list.Capacity = count;
            }

            for (int i = 0; i < count; ++i)
            {
                T value = default;
                readDelegate(reader, ref value, ctx);
                list.Add(value);
            }
        }

        public void Write([NotNull] BinaryWriter writer, ref List<T> list, [NotNull] SerializationContext ctx)
        {
            var writeDelegate = SerializationRegistry<T>.GetWrite();
            if (writeDelegate == null)
                return;

            writer.Write(list.Count);
            foreach (var item in list)
            {
                var value = item;
                writeDelegate(writer, ref value, ctx);
            }
        }
    }
}
#endif
