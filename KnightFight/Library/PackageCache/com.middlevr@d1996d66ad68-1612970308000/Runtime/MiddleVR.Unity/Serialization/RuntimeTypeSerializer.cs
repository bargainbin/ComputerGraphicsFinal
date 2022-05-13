/* RuntimeTypeSerializer
 * MiddleVR
 * (c) MiddleVR
 */

#if !ENABLE_IL2CPP
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace MiddleVR.Unity.Serialization
{
    /// <summary>
    /// Serialize the contents of an object or a non-blittable struct.
    ///
    /// Dynamically creates a reader/writer serializer that serializes all the object's fields.
    /// </summary>
    public static class RuntimeTypeSerializer
    {
        public static ISerializer<T> FromType<T>()
        {
            return new CustomSerializer<T>(MakeWriteExpression<T>().Compile(), MakeReadExpression<T>().Compile());
        }

        private static Expression<SerializationRegistry.WriteDelegate<T>> MakeWriteExpression<T>()
        {
            var instanceFields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var writerParam = Expression.Parameter(typeof(BinaryWriter), "writer");
            var valParam = Expression.Parameter(typeof(T).MakeByRefType(), "val");
            var ctxParam = Expression.Parameter(typeof(SerializationContext), "ctx");

            var expressions = new List<Expression>();

            foreach (var field in instanceFields)
            {
                if (!SerializationRegistry.IsFieldSerializable(field))
                    continue;

                // For each field, generate an expression that does:

                // {
                //     var writeDelegate = SerializerRegistry<{field}.FieldType>.GetWrite();
                //     if (writeDelegate != null)
                //     {
                //         writeDelegate(writer, ref {field}, ctx);
                //     }
                // }

                var delegateToGetWrite = SerializationRegistry.GetDelegateToGetWrite(field.FieldType);
                // We can't actually use the `var` keyword so we deduce the return type ourselves
                var writeDelegateType = SerializationRegistry.GetReturnTypeOfGetWrite(field.FieldType);
                var writeDelegateVariable = Expression.Variable(writeDelegateType, field.Name + "WriterDelegate");
                var writeDelegateExpr = Expression.Convert(Expression.Invoke(Expression.Constant(delegateToGetWrite)), writeDelegateType);
                expressions.Add(Expression.Block(new[] { writeDelegateVariable },
                    Expression.Assign(writeDelegateVariable, writeDelegateExpr),
                    Expression.IfThen(
                        Expression.NotEqual(writeDelegateVariable, Expression.Constant(null)),
                        Expression.Invoke(writeDelegateVariable, writerParam, Expression.Field(valParam, field.Name), ctxParam))));
            }

            return (Expression<SerializationRegistry.WriteDelegate<T>>)
                Expression.Lambda(
                    typeof(SerializationRegistry.WriteDelegate<T>),
                    Expression.Block(expressions),
                    writerParam,
                    valParam,
                    ctxParam);
        }

        private static Expression<SerializationRegistry.ReadDelegate<T>> MakeReadExpression<T>()
        {
            var instanceFields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var readerParam = Expression.Parameter(typeof(BinaryReader), "reader");
            var valParam = Expression.Parameter(typeof(T).MakeByRefType(), "val");
            var ctxParam = Expression.Parameter(typeof(SerializationContext), "ctx");

            var expressions = new List<Expression>();

            foreach (var field in instanceFields)
            {
                if (!SerializationRegistry.IsFieldSerializable(field))
                    continue;

                // For each field, generate an expression that does:

                // {
                //     var readDelegate = SerializerRegistry<{field}.FieldType>.GetRead();
                //     if (readDelegate != null)
                //     {
                //         readDelegate(reader, ref {field}, ctx);
                //     }
                // }

                var delegateToGetRead = SerializationRegistry.GetDelegateToGetRead(field.FieldType);
                // We can't actually use the `var` keyword so we deduce the return type ourselves
                var readDelegateType = SerializationRegistry.GetReturnTypeOfGetRead(field.FieldType);
                var readDelegateVariable = Expression.Variable(readDelegateType, field.Name + "ReaderDelegate");
                var readDelegateExpr = Expression.Convert(Expression.Invoke(Expression.Constant(delegateToGetRead)), readDelegateType);
                expressions.Add(Expression.Block(new[] { readDelegateVariable },
                    Expression.Assign(readDelegateVariable, readDelegateExpr),
                    Expression.IfThen(
                        Expression.NotEqual(readDelegateVariable, Expression.Constant(null)),
                        Expression.Invoke(readDelegateVariable, readerParam, Expression.Field(valParam, field.Name), ctxParam))));
            }

            return (Expression<SerializationRegistry.ReadDelegate<T>>)
                Expression.Lambda(
                    typeof(SerializationRegistry.ReadDelegate<T>),
                    Expression.Block(expressions),
                    readerParam,
                    valParam,
                    ctxParam);
        }
    }
}
#endif
