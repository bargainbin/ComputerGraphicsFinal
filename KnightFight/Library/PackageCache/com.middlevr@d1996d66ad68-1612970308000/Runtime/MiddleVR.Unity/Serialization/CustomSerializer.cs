/* CustomSerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.IO;
using JetBrains.Annotations;

namespace MiddleVR.Unity.Serialization
{
    public class CustomSerializer<T> : ISerializer<T>
    {
        private readonly SerializationRegistry.WriteDelegate<T> _serialize;
        private readonly SerializationRegistry.ReadDelegate<T> _deserialize;

        public CustomSerializer(SerializationRegistry.WriteDelegate<T> serialize, SerializationRegistry.ReadDelegate<T> deserialize)
        {
            _serialize = serialize;
            _deserialize = deserialize;
        }

        public void Read([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx)
        {
            _deserialize(reader, ref val, ctx);
        }

        public void Write([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx)
        {
            _serialize(writer, ref val, ctx);
        }
    }
}
#endif
