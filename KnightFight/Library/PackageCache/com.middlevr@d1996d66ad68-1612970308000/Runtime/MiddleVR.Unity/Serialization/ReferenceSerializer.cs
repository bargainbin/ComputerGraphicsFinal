/* ReferenceSerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialize a reference to an object.
    ///
    /// Handles null references, reference cycle checking, and references
    /// to derived types.
    /// </summary>
    public static class ReferenceSerializer
    {
        /// <summary>
        /// Create a instance of ReferenceSerializer<T> using reflection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Instance of ReferenceSerializer<T></returns>
        public static ISerializer<T> FromType<T>()
        {
            var type = typeof(T);

            // Check that the type matches the serializer constraints (T : class, new())
            if (!type.GetTypeInfo().IsClass ||
                type.GetConstructor(Type.EmptyTypes) == null)
                return null;

            var serializerType = typeof(ReferenceSerializer<>).MakeGenericType(type);
            return (ISerializer<T>)Activator.CreateInstance(serializerType);
        }
    }

    internal class ReferenceSerializer<T> : ISerializer<T>
        where T : class, new()
    {
        enum ReferenceType : byte
        {
            Null = 0,        // Source field contains a null reference
            Loop = 1,        // Source field contains a non-null reference but we are in a reference loop so we skip it
            ExactType = 2,   // Source field references a non-null reference to an object of its exact type 
            DerivedType = 3, // Source field references a non-null reference to an object of a derived type
        }

        private delegate void ReadRealTypeDelegate([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx);
        private static readonly Dictionary<Type, ReadRealTypeDelegate> s_readRealTypeCache = new Dictionary<Type, ReadRealTypeDelegate>();

        private static void ReadRealType<R>([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx) where R : T
        {
            var realVal = (R)val;
            SerializationRegistry<R>.GetReadContent()(reader, ref realVal, ctx);
        }

        private ReadRealTypeDelegate GetReadRealType(Type t)
        {
            if (!s_readRealTypeCache.TryGetValue(t, out var readRealTypeDelegate))
            {
                readRealTypeDelegate = (ReadRealTypeDelegate)(typeof(ReferenceSerializer<T>)
                    .GetMethod(nameof(ReadRealType), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(t)
                    .CreateDelegate(typeof(ReadRealTypeDelegate)));
                s_readRealTypeCache.Add(t, readRealTypeDelegate);
            }

            return readRealTypeDelegate;
        }

        public void Read([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx)
        {
            var referenceType = (ReferenceType)reader.ReadByte();
            switch (referenceType)
            {
                case ReferenceType.Null:
                    val = null;
                    break;

                case ReferenceType.Loop:
                    break;

                case ReferenceType.ExactType:
                    if (val == null)
                        val = new T();
                    SerializationRegistry<T>.GetReadContent()(reader, ref val, ctx);
                    break;

                case ReferenceType.DerivedType:
                    var assemblyQualifiedName = reader.ReadString();
                    var realType = Type.GetType(assemblyQualifiedName);
                    if (val == null)
                        val = (T)Activator.CreateInstance(realType);
                    GetReadRealType(realType)(reader, ref val, ctx);
                    break;
            }
        }

        private delegate void WriteRealTypeDelegate([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx);
        private static readonly Dictionary<Type, WriteRealTypeDelegate> s_writeRealTypeCache = new Dictionary<Type, WriteRealTypeDelegate>();

        private static void WriteRealType<R>([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx) where R : T
        {
            var realVal = (R)val;
            SerializationRegistry<R>.GetWriteContent()(writer, ref realVal, ctx);
        }

        private WriteRealTypeDelegate GetWriteRealType(Type t)
        {
            if (!s_writeRealTypeCache.TryGetValue(t, out var writeRealTypeDelegate))
            {
                writeRealTypeDelegate = (WriteRealTypeDelegate)(typeof(ReferenceSerializer<T>)
                    .GetMethod(nameof(WriteRealType), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(t)
                    .CreateDelegate(typeof(WriteRealTypeDelegate)));
                s_writeRealTypeCache.Add(t, writeRealTypeDelegate);
            }

            return writeRealTypeDelegate;
        }

        public void Write([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx)
        {
            if (val == null)
            {
                writer.Write((byte)ReferenceType.Null);
                return;
            }

            if (ctx.Contains(val))
            {
                writer.Write((byte)ReferenceType.Loop);
                return;
            }

            try
            {
                ctx.Push(val);

                var realType = val.GetType();
                if (realType == typeof(T))
                {
                    writer.Write((byte)ReferenceType.ExactType);
                    SerializationRegistry<T>.GetWriteContent()(writer, ref val, ctx);
                }
                else
                {
                    writer.Write((byte)ReferenceType.DerivedType);
                    writer.Write(realType.AssemblyQualifiedName);
                    GetWriteRealType(realType)(writer, ref val, ctx);
                }
            }
            finally
            {
                ctx.Pop();
            }
        }
    }
}
#endif
