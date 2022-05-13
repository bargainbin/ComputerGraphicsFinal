/* SerializationRegistry
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialization core
    /// </summary>
    public static class SerializationRegistry
    {
        #region Basic delegate types for serialization
        public delegate void WriteDelegate<T>([NotNull] BinaryWriter writer, ref T val, [NotNull] SerializationContext ctx);
        public delegate void ReadDelegate<T>([NotNull] BinaryReader reader, ref T val, [NotNull] SerializationContext ctx);
        #endregion

        #region Type serialization policy
        private static class IsTypeSerializableGenericCache<T>
        {
            public static readonly bool Value = IsTypeSerializable(typeof(T));
        }

        public static bool IsTypeSerializable<T>()
        {
            return IsTypeSerializableGenericCache<T>.Value;
        }

        public static bool IsTypeSerializable(Type typeToSerialize)
        {
            return !typeToSerialize.IsPointer
                && !typeToSerialize.IsInterface
                && !typeToSerialize.IsGenericTypeDefinition
                && ((typeToSerialize.IsPrimitive && typeToSerialize.IsValueType) ||
                    BlittableSerializer.IsUnmanaged(typeToSerialize) ||
                    // Allow all types marked as [Serializable]
                    typeToSerialize.GetCustomAttribute<SerializableAttribute>() != null ||
                    // These unity types are only allowed as top-level types
                    typeToSerialize == typeof(MonoBehaviour) ||
                    typeToSerialize.IsSubclassOf(typeof(MonoBehaviour)) ||
                    typeToSerialize == typeof(ScriptableObject) ||
                    typeToSerialize.IsSubclassOf(typeof(ScriptableObject)));
        }

        internal static bool IsFieldSerializable(FieldInfo field)
        {
            return
                // Check if type meets basic serialization check
                IsTypeSerializable(field.FieldType) &&
                // Skip references to Unity Objects as we probably don't know how to serialize them
                !field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) &&
                // Should we serialize the field based on its attributes ?
                ((field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null) ||
                (!field.IsPublic && field.GetCustomAttribute<SerializeField>() != null));
        }
        #endregion

        #region Type-erased serializer getters for RuntimeTypeSerializer
        internal delegate Delegate DelegateToGetWrite();
        private static readonly Dictionary<Type, Type> s_returnTypeOfGetWriteCache = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, DelegateToGetWrite> s_delegateToGetWriteCache = new Dictionary<Type, DelegateToGetWrite>();

        /// <summary>
        /// Get the return type of SerializationRegistry<T>.GetWrite()
        /// </summary>
        internal static Type GetReturnTypeOfGetWrite(Type typeToSerialize)
        {
            if (!s_returnTypeOfGetWriteCache.TryGetValue(typeToSerialize, out var returnTypeOfGetWrite))
            {
                returnTypeOfGetWrite = typeof(WriteDelegate<>).MakeGenericType(typeToSerialize);
                s_returnTypeOfGetWriteCache.Add(typeToSerialize, returnTypeOfGetWrite);
            }

            return returnTypeOfGetWrite;
        }

        /// <summary>
        /// Get a type-erased delegate to SerializationRegistry<T>.GetWrite()
        /// </summary>
        internal static DelegateToGetWrite GetDelegateToGetWrite(Type typeToSerialize)
        {
            if (!s_delegateToGetWriteCache.TryGetValue(typeToSerialize, out var delegateToGetWrite))
            {
                delegateToGetWrite = (DelegateToGetWrite)(typeof(SerializationRegistry<>)
                    .MakeGenericType(typeToSerialize)
                    .GetMethod(nameof(SerializationRegistry<object>.GetWrite), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .CreateDelegate(typeof(DelegateToGetWrite)));

                s_delegateToGetWriteCache.Add(typeToSerialize, delegateToGetWrite);
            }

            return delegateToGetWrite;
        }

        internal delegate Delegate DelegateToGetRead();
        private static readonly Dictionary<Type, Type> s_returnTypeOfGetReadCache = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, DelegateToGetRead> s_delegateToGetReadCache = new Dictionary<Type, DelegateToGetRead>();

        /// <summary>
        /// Get the return type of SerializationRegistry<T>.GetRead()
        /// </summary>
        internal static Type GetReturnTypeOfGetRead(Type typeToSerialize)
        {
            if (!s_returnTypeOfGetReadCache.TryGetValue(typeToSerialize, out var returnTypeOfGetRead))
            {
                returnTypeOfGetRead = typeof(ReadDelegate<>).MakeGenericType(typeToSerialize);
                s_returnTypeOfGetReadCache.Add(typeToSerialize, returnTypeOfGetRead);
            }

            return returnTypeOfGetRead;
        }

        /// <summary>
        /// Get a type-erased delegate to SerializationRegistry<T>.GetRead()
        /// </summary>
        internal static DelegateToGetRead GetDelegateToGetRead(Type typeToSerialize)
        {
            if (!s_delegateToGetReadCache.TryGetValue(typeToSerialize, out var delegateToGetRead))
            {
                delegateToGetRead = (DelegateToGetRead)(typeof(SerializationRegistry<>)
                    .MakeGenericType(typeToSerialize)
                    .GetMethod(nameof(SerializationRegistry<object>.GetRead), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .CreateDelegate(typeof(DelegateToGetRead)));

                s_delegateToGetReadCache.Add(typeToSerialize, delegateToGetRead);
            }

            return delegateToGetRead;
        }
        #endregion
    }

    /// <summary>
    /// Serialization core (type-specific)
    /// </summary>
    public static class SerializationRegistry<T>
    {
        private static bool s_created = false;
        private static ISerializer<T> s_contentSerializer = null;
        private static ISerializer<T> s_serializer = null;

        /// <summary>
        /// Serializer for the value/object content
        /// (does not handle polymorphism:
        /// expects a non-null reference of the real type
        /// of the referenced object)
        /// This is used by ReferenceSerializer.
        /// </summary>
        public static ISerializer<T> ContentSerializer
        {
            get
            {
                if (!s_created)
                    CreateSerializer();

                return s_contentSerializer;
            }
        }

        /// <summary>
        /// Serializer for a value/object
        /// (handles null/not-null/derived types)
        /// </summary>
        public static ISerializer<T> Serializer
        {
            get
            {
                if (!s_created)
                    CreateSerializer();

                return s_serializer;
            }
        }

        /// <summary>
        /// Create an appropriate serializer for this type.
        /// </summary>
        private static void CreateSerializer()
        {
            s_created = true;

            var typeToSerialize = typeof(T);
            var typeToSerializeInfo = typeToSerialize.GetTypeInfo();

            if (typeToSerialize.IsGenericTypeDefinition)
                throw new ArgumentException("T must not be a generic type definition");

            // 1- Check collection types
            if (typeToSerialize.IsGenericType)
            {
                // Exceptions for Dictionary and List, they do not need to be serializable
                var genericDefinition = typeToSerialize.GetGenericTypeDefinition();
                if (genericDefinition == typeof(Dictionary<,>))
                {
                    var dictionarySerializerType = typeof(DictionarySerializer<,>).MakeGenericType(typeToSerialize.GetGenericArguments());
                    var referenceSerializerType = typeof(ReferenceSerializer<>).MakeGenericType(typeof(T));
                    s_contentSerializer = (ISerializer<T>)Activator.CreateInstance(dictionarySerializerType);
                    s_serializer = (ISerializer<T>)Activator.CreateInstance(referenceSerializerType);
                    return;
                }
                else if (genericDefinition == typeof(List<>))
                {
                    var listSerializerType = typeof(ListSerializer<>).MakeGenericType(typeToSerialize.GetGenericArguments());
                    var referenceSerializerType = typeof(ReferenceSerializer<>).MakeGenericType(typeof(T));
                    s_contentSerializer = (ISerializer<T>)Activator.CreateInstance(listSerializerType);
                    s_serializer = (ISerializer<T>)Activator.CreateInstance(referenceSerializerType);
                    return;
                }
            }

            // 2- Check if type is serializable at all
            if (!SerializationRegistry.IsTypeSerializable(typeToSerialize))
                return;

            // 3- Check for string
            if (typeof(T) == typeof(string))
            {
                var serializer = (ISerializer<T>)Activator.CreateInstance(typeof(StringSerializer));

                s_contentSerializer = serializer;
                s_serializer = serializer;
                return;
            }

            // 4- Check if type is blittable
            if (BlittableSerializer.IsUnmanaged(typeToSerialize))
            {
                var serializer = BlittableSerializer.FromType<T>();

                s_contentSerializer = serializer;
                s_serializer = serializer;
                return;
            }

            // 5- Check if type is a non-blittable value (struct)
            if (typeToSerializeInfo.IsValueType)
            {
                var serializer = RuntimeTypeSerializer.FromType<T>();

                s_contentSerializer = serializer;
                s_serializer = serializer;
                return;
            }

            // 6- Check if type is a reference type
            if (typeToSerializeInfo.IsClass)
            {
                var serializer = ReferenceSerializer.FromType<T>();
                var contentSerializer = RuntimeTypeSerializer.FromType<T>();

                s_contentSerializer = contentSerializer;
                s_serializer = serializer ?? contentSerializer;
                return;
            }

        }

        /// <summary>
        /// Register a custom serializer
        /// </summary>
        /// <param name="contentSerializer"></param>
        /// <param name="referenceSerializer"></param>
        public static void Register(ISerializer<T> contentSerializer, ISerializer<T> referenceSerializer = null)
        {
            s_created = true;
            s_contentSerializer = contentSerializer;
            s_serializer = referenceSerializer ?? contentSerializer; // (x ?? y) <=> (x != null ? x : y)
        }

        internal static SerializationRegistry.WriteDelegate<T> GetWriteContent()
        {
            var serializerInterface = ContentSerializer;
            if (serializerInterface == null)
                return null;
            return serializerInterface.Write;
        }

        internal static SerializationRegistry.ReadDelegate<T> GetReadContent()
        {
            var serializerInterface = ContentSerializer;
            if (serializerInterface == null)
                return null;
            return serializerInterface.Read;
        }

        internal static SerializationRegistry.WriteDelegate<T> GetWrite()
        {
            var serializerInterface = Serializer;
            if (serializerInterface == null)
                return null;
            return serializerInterface.Write;
        }

        internal static SerializationRegistry.ReadDelegate<T> GetRead()
        {
            var serializerInterface = Serializer;
            if (serializerInterface == null)
                return null;
            return serializerInterface.Read;
        }
    }
}
#endif
