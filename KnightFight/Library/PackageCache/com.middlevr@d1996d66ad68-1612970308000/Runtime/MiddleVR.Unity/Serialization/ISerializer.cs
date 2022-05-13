/* ISerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.IO;
using JetBrains.Annotations;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serializer interface
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    public interface ISerializer<T>
    {
        void Write([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx);
        void Read([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx);
    }
}
#endif
