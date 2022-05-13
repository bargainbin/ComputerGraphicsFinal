/* StringSerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.IO;
using JetBrains.Annotations;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialize a string
    /// </summary>
    public class StringSerializer : ISerializer<string>
    {
        private enum ReferenceType
        {
            Null = 0,
            NotNull = 1
        }

        public void Read([NotNull] BinaryReader reader, ref string val, [NotNull] SerializationContext ctx)
        {
            var refType = (ReferenceType)reader.ReadByte();
            switch (refType)
            {
                case ReferenceType.Null:
                    val = null;
                    break;
                case ReferenceType.NotNull:
                    val = reader.ReadString();
                    break;
            }
        }

        public void Write([NotNull] BinaryWriter writer, ref string val, [NotNull] SerializationContext ctx)
        {
            if (val == null)
            {
                writer.Write((byte)ReferenceType.Null);
            }
            else
            {
                writer.Write((byte)ReferenceType.NotNull);
                writer.Write(val);
            }
        }
    }
}
#endif
