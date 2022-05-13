/* Cluster
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MiddleVR.Unity.Utils;
using UnityEngine;

namespace MiddleVR.Unity
{
    /// <summary>
    /// Synchronize messages across a MiddleVR cluster
    /// </summary>
    public class Cluster
    {
        public static bool IsServer => MVR.ClusterMgr.IsServer();
        public static bool IsClient => MVR.ClusterMgr.IsClient();

        #region Public Methods: Server synchronized messages
        public static void AddMessageHandlerRaw(object obj, Action<IntPtr, int> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddServerMsgMessageHandlerInternal(obj, null, handler, channel, addCleanBehaviour);
        }

        public static void AddMessageHandlerRaw<TEnum>(object obj, Action<IntPtr, int> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddMessageHandlerRaw(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddMessageHandler(object obj, Action<string> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddServerMsgMessageHandlerInternal(obj, typeof(string), (IntPtr ptr, int size) =>
            {
                unsafe
                {
                    handler(Encoding.UTF8.GetString((byte*)ptr, size));
                }
            }, channel, addCleanBehaviour);
        }

        public static void AddMessageHandler<TEnum>(object obj, Action<string> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddMessageHandler(object obj, Action<byte[]> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddServerMsgMessageHandlerInternal(obj, typeof(ArraySegment<byte>), (IntPtr ptr, int size) =>
            {
                var data = new byte[size];
                Marshal.Copy(ptr, data, 0, size);
                handler(data);
            }, channel, addCleanBehaviour);
        }

        private static readonly MemoryStream s_readerMemoryStream = new MemoryStream();
        private static readonly BinaryReader s_reader = new BinaryReader(s_readerMemoryStream);

        // Alternative for receiving byte arrays : MemoryStream
        public static void AddMessageHandler<TEnum>(object obj, Action<MemoryStream> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddMessageHandler(object obj, Action<MemoryStream> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddServerMsgMessageHandlerInternal(obj, typeof(ArraySegment<byte>), (IntPtr ptr, int size) =>
            {
                s_readerMemoryStream.Seek(0, SeekOrigin.Begin);
                s_readerMemoryStream.SetLength(size);
                Marshal.Copy(ptr, s_readerMemoryStream.GetBuffer(), 0, size);
                handler(s_readerMemoryStream);
            }, channel, addCleanBehaviour);
        }

        // Alternative for receiving byte arrays : BinaryReader
        public static void AddMessageHandler<TEnum>(object obj, Action<BinaryReader> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddMessageHandler(object obj, Action<BinaryReader> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddServerMsgMessageHandlerInternal(obj, typeof(ArraySegment<byte>), (IntPtr ptr, int size) =>
            {
                s_readerMemoryStream.Seek(0, SeekOrigin.Begin);
                s_readerMemoryStream.SetLength(size);
                Marshal.Copy(ptr, s_readerMemoryStream.GetBuffer(), 0, size);
                handler(s_reader);
            }, channel, addCleanBehaviour);
        }

        public static void AddMessageHandler<TEnum>(object obj, Action<byte[]> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddMessageHandler<T>(object obj, Action<T> handler, int channel = 0, bool addCleanBehaviour = true)
            where T : unmanaged
        {
            AddServerMsgMessageHandlerInternal(obj, typeof(T), (IntPtr ptr, int size) =>
            {
                handler(UnsafeUtils.UnmanagedFromBufferAligned<T>(ptr));
            }, channel, addCleanBehaviour);
        }

        public static void AddMessageHandler<T, TEnum>(object obj, Action<T> handler, TEnum channel, bool addCleanBehaviour = true)
            where T : unmanaged
            where TEnum : unmanaged, Enum
        {
            AddMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void RemoveMessageHandler(object obj, int channel)
        {
            if (!GetServerMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
                return;

            channelToHandlerDict.Remove(channel);

            if (channelToHandlerDict.Count == 0 && objectId != 0
                && s_objectIdToChannelToClientMsgHandlerDict[objectId].Count == 0)
            {
                // Do not remove object null/id 0
                RemoveObjectMapping(obj, objectId);
            }
        }

        public static void RemoveMessageHandler<TEnum>(object obj, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            RemoveMessageHandler(obj, UnsafeUtils.EnumToInt32(channel));
        }

        public static void Remove(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", $"{nameof(Cluster)}: Cannot remove null object. Use {nameof(RemoveMessageHandler)} instead.");

            RemoveObjectMapping(obj);
        }

        public static void BroadcastMessageRaw(object obj, IntPtr ptr, int size, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckServerMsgHandler(obj, channel, null);

            var buf = clusterMgr.PrepareSynchronizedMessage(size);
            if (buf == IntPtr.Zero)
                return;

            unsafe
            {
                Buffer.MemoryCopy((void*)ptr, (void*)buf, size, size);
            }

            clusterMgr.SendPreparedSynchronizedMessage(objectId, channel);
        }

        public static void BroadcastMessageRaw<TEnum>(object obj, IntPtr ptr, int size, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            BroadcastMessageRaw(obj, ptr, size, UnsafeUtils.EnumToInt32(channel));
        }

        public static void BroadcastMessage(object obj, string s, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckServerMsgHandler(obj, channel, typeof(string));

            unsafe
            {
                fixed (char* inPtr = s)
                {
                    var inLength = s.Length;
                    var outLength = Encoding.UTF8.GetByteCount(inPtr, inLength);

                    var outBuf = clusterMgr.PrepareSynchronizedMessage(outLength);
                    if (outBuf == IntPtr.Zero)
                        return;

                    byte* outPtr = (byte*)outBuf;
                    Encoding.UTF8.GetBytes(inPtr, inLength, outPtr, outLength);
                }
            }

            clusterMgr.SendPreparedSynchronizedMessage(objectId, channel);
        }

        public static void BroadcastMessage<TEnum>(object obj, string s, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            BroadcastMessage(obj, s, UnsafeUtils.EnumToInt32(channel));
        }

        public static void BroadcastMessage(object obj, ArraySegment<byte> data, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckServerMsgHandler(obj, channel, typeof(ArraySegment<byte>));

            var buf = clusterMgr.PrepareSynchronizedMessage(data.Count);
            if (buf == IntPtr.Zero)
                return;

            Marshal.Copy(data.Array, data.Offset, buf, data.Count);

            clusterMgr.SendPreparedSynchronizedMessage(objectId, channel);
        }

        public static void BroadcastMessage<TEnum>(object obj, ArraySegment<byte> data, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            BroadcastMessage(obj, data, UnsafeUtils.EnumToInt32(channel));
        }

        public static void BroadcastMessage(object obj, byte[] data, int channel = 0)
        {
            BroadcastMessage(obj, new ArraySegment<byte>(data), channel);
        }

        public static void BroadcastMessage<TEnum>(object obj, byte[] data, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            BroadcastMessage(obj, new ArraySegment<byte>(data), UnsafeUtils.EnumToInt32(channel));
        }

        public static void BroadcastMessage(object obj, MemoryStream ms, int channel = 0)
        {
            if (!ms.TryGetBuffer(out var arraySegment))
                throw new InvalidOperationException("MemoryStream buffer must be accessible");
            BroadcastMessage(obj, arraySegment, channel);
        }

        public static void BroadcastMessage<TEnum>(object obj, MemoryStream data, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            BroadcastMessage(obj, data, UnsafeUtils.EnumToInt32(channel));
        }

        public static void BroadcastMessage<T>(object obj, T value, int channel = 0)
            where T : unmanaged
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckServerMsgHandler(obj, channel, typeof(T));

            var outBuf = clusterMgr.PrepareSynchronizedMessage(UnsafeUtils.SizeOfUnmanaged<T>());
            if (outBuf == IntPtr.Zero)
                return;

            UnsafeUtils.UnmanagedToBufferAligned(outBuf, value);

            clusterMgr.SendPreparedSynchronizedMessage(objectId, channel);
        }

        public static void BroadcastMessage<T, TEnum>(object obj, T value, TEnum channel)
            where T : unmanaged
            where TEnum : unmanaged, Enum
        {
            BroadcastMessage(obj, value, UnsafeUtils.EnumToInt32(channel));
        }
        #endregion

        #region Public Methods: Client asynchronous messages
        public static void AddClientMessageHandlerRaw(object obj, Action<int, IntPtr, int> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddClientMessageHandlerInternal(obj, null, handler, channel, addCleanBehaviour);
        }

        public static void AddClientMessageHandlerRaw<TEnum>(object obj, Action<int, IntPtr, int> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddClientMessageHandlerRaw(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddClientMessageHandler(object obj, Action<int, string> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddClientMessageHandlerInternal(obj, typeof(string), (int clientId, IntPtr ptr, int size) =>
            {
                unsafe
                {
                    handler(clientId, Encoding.UTF8.GetString((byte*)ptr, size));
                }
            }, channel, addCleanBehaviour);
        }

        public static void AddClientMessageHandler<TEnum>(object obj, Action<int, string> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddClientMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddClientMessageHandler(object obj, Action<int, byte[]> handler, int channel = 0, bool addCleanBehaviour = true)
        {
            AddClientMessageHandlerInternal(
                obj,
                typeof(byte[]),
                (int clientId, IntPtr ptr, int size) =>
                    {
                        var data = new byte[size];
                        Marshal.Copy(ptr, data, 0, size);
                        handler(clientId, data);
                    },
                channel,
                addCleanBehaviour);
        }

        public static void AddClientMessageHandler<TEnum>(object obj, Action<int, byte[]> handler, TEnum channel, bool addCleanBehaviour = true)
            where TEnum : unmanaged, Enum
        {
            AddClientMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void AddClientMessageHandler<T>(object obj, Action<int, T> handler, int channel = 0, bool addCleanBehaviour = true)
            where T : unmanaged
        {
            AddClientMessageHandlerInternal(
                    obj,
                    typeof(T),
                    (int clientId, IntPtr ptr, int size) =>
                        {
                            handler(clientId, UnsafeUtils.UnmanagedFromBufferAligned<T>(ptr));
                        },
                    channel,
                    addCleanBehaviour);
        }

        public static void AddClientMessageHandler<T, TEnum>(object obj, Action<int, T> handler, TEnum channel, bool addCleanBehaviour = true)
            where T : unmanaged
            where TEnum : unmanaged, Enum
        {
            AddClientMessageHandler(obj, handler, UnsafeUtils.EnumToInt32(channel), addCleanBehaviour);
        }

        public static void RemoveClientMessageHandler(object obj, int channel)
        {
            if (!GetClientMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
                return;

            channelToHandlerDict.Remove(channel);

            if (channelToHandlerDict.Count == 0 && objectId != 0
                && s_objectIdToChannelToServerMsgHandlerDict[objectId].Count == 0 )
            {
                // Do not remove object null/id 0
                RemoveObjectMapping(obj, objectId);
            }
        }

        public static void RemoveClientMessageHandler<TEnum>(object obj, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            RemoveClientMessageHandler(obj, UnsafeUtils.EnumToInt32(channel));
        }

        public static void SendAsyncMessageToServerRaw(object obj, IntPtr ptr, int size, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckClientMsgHandler(obj, channel, null);

            var buf = clusterMgr.PrepareAsyncClientMessage(size);
            if (buf == IntPtr.Zero)
                return;

            unsafe
            {
                Buffer.MemoryCopy((void*)ptr, (void*)buf, size, size);
            }

            clusterMgr.SendPreparedAsyncClientMessage(objectId, channel);
        }

        public static void SendAsycMessageToServerRaw<TEnum>(object obj, IntPtr ptr, int size, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            SendAsyncMessageToServerRaw(obj, ptr, size, UnsafeUtils.EnumToInt32(channel));
        }

        public static void SendAsyncMessageToServer(object obj, string s, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckClientMsgHandler(obj, channel, typeof(string));

            unsafe
            {
                fixed (char* inPtr = s)
                {
                    var inLength = s.Length;
                    var outLength = Encoding.UTF8.GetByteCount(inPtr, inLength);

                    var outBuf = clusterMgr.PrepareAsyncClientMessage(outLength);
                    if (outBuf == IntPtr.Zero)
                        return;

                    byte* outPtr = (byte*)outBuf;
                    Encoding.UTF8.GetBytes(inPtr, inLength, outPtr, outLength);
                }
            }

            clusterMgr.SendPreparedAsyncClientMessage(objectId, channel);
        }

        public static void SendAsyncMessageToServer<TEnum>(object obj, string s, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            SendAsyncMessageToServer(obj, s, UnsafeUtils.EnumToInt32(channel));
        }

        public static void SendAsyncMessageToServer(object obj, byte[] data, int channel = 0)
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckClientMsgHandler(obj, channel, typeof(byte[]));

            var buf = clusterMgr.PrepareAsyncClientMessage(data.Length);
            if (buf == IntPtr.Zero)
                return;

            Marshal.Copy(data, 0, buf, data.Length);

            clusterMgr.SendPreparedAsyncClientMessage(objectId, channel);
        }

        public static void SendAsyncMessageToServer<TEnum>(object obj, byte[] data, TEnum channel)
            where TEnum : unmanaged, Enum
        {
            SendAsyncMessageToServer(obj, data, UnsafeUtils.EnumToInt32(channel));
        }

        public static void SendAsyncMessageToServer<T>(object obj, T value, int channel = 0)
            where T : unmanaged
        {
            var clusterMgr = CheckClusterManager();
            var objectId = CheckServerMsgHandler(obj, channel, typeof(T));

            var outBuf = clusterMgr.PrepareAsyncClientMessage(UnsafeUtils.SizeOfUnmanaged<T>());
            if (outBuf == IntPtr.Zero)
                return;

            UnsafeUtils.UnmanagedToBufferAligned(outBuf, value);

            clusterMgr.SendPreparedAsyncClientMessage(objectId, channel);
        }

        public static void SendAsyncMessageToServer<T, TEnum>(object obj, T value, TEnum channel)
            where T : unmanaged
            where TEnum : unmanaged, Enum
        {
            SendAsyncMessageToServer(obj, value, UnsafeUtils.EnumToInt32(channel));
        }
        #endregion

        #region Public methods
        public static void DispatchMessages()
        {
            var clusterMgr = CheckClusterManager();

            while (clusterMgr.ReceiveSynchronizedMessage(out int objectId, out int channel, out IntPtr ptr, out int size))
            {
                if (!GetServerMsgChannelToHandlerDict(objectId, out var channelToHandlerDict))
                {
                    Debug.LogWarning($"{nameof(Cluster)}: No registered handler for object id {objectId}");
                    continue;
                }

                if (!channelToHandlerDict.TryGetValue(channel, out var handlerInfo))
                {
                    Debug.LogWarning($"{nameof(Cluster)}: No registered handler for object id {objectId} channel {channel}");
                    continue;
                }

                try
                {
                    handlerInfo.Handler(ptr, size);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            while (clusterMgr.ReceiveAsyncClientMessage(out int clientId, out int objectId, out int channel, out IntPtr ptr, out int size))
            {
                if (!GetClientMsgChannelToHandlerDict(objectId, out var channelToHandlerDict))
                {
                    Debug.LogWarning($"{nameof(Cluster)}: No registered handler for object id {objectId}");
                    continue;
                }

                if (!channelToHandlerDict.TryGetValue(channel, out var handlerInfo))
                {
                    Debug.LogWarning($"{nameof(Cluster)}: No registered handler for object id {objectId} channel {channel}");
                    continue;
                }

                try
                {
                    handlerInfo.Handler(clientId, ptr, size);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        #endregion

        #region Private : Data
        // object <-> id mapping
        private static int s_nextId = 1;
        private static readonly Dictionary<object, int> s_objectToObjectIdDict = new Dictionary<object, int>();

        // (object id, channel) -> handler mapping
        private struct ServerMessageHandlerInfo
        {
            public Action<IntPtr, int> Handler;
            public Type ArgumentType;
        }

        private static readonly Dictionary<int, Dictionary<int, ServerMessageHandlerInfo>> s_objectIdToChannelToServerMsgHandlerDict =
            new Dictionary<int, Dictionary<int, ServerMessageHandlerInfo>> { { 0, new Dictionary<int, ServerMessageHandlerInfo>() } };

        private struct ClientMessageHandlerInfo
        {
            public Action<int, IntPtr, int> Handler;
            public Type ArgumentType;
        }

        private static readonly Dictionary<int, Dictionary<int, ClientMessageHandlerInfo>> s_objectIdToChannelToClientMsgHandlerDict =
            new Dictionary<int, Dictionary<int, ClientMessageHandlerInfo>> { { 0, new Dictionary<int, ClientMessageHandlerInfo>() } };

        #endregion

        #region Private : Methods
        private static void AddObjectMapping(object obj, out int objectId)
        {
            objectId = s_nextId++;
            s_objectToObjectIdDict.Add(obj, objectId);

            var serverMsgChannelToHandlerDict = new Dictionary<int, ServerMessageHandlerInfo>();
            s_objectIdToChannelToServerMsgHandlerDict.Add(objectId, serverMsgChannelToHandlerDict);

            var clientMsgChannelToHandlerDict = new Dictionary<int, ClientMessageHandlerInfo>();
            s_objectIdToChannelToClientMsgHandlerDict.Add(objectId, clientMsgChannelToHandlerDict);
        }

        private static void RemoveObjectMapping(object obj, int objectId)
        {
            s_objectToObjectIdDict.Remove(obj);

            s_objectIdToChannelToServerMsgHandlerDict.Remove(objectId);
            s_objectIdToChannelToClientMsgHandlerDict.Remove(objectId);
        }

        private static void RemoveObjectMapping(object obj)
        {
            if (obj != null && s_objectToObjectIdDict.TryGetValue(obj, out int objectId))
            {
                RemoveObjectMapping(obj, objectId);
            }
        }

        private static bool GetServerMsgChannelToHandlerDict(int objectId, out Dictionary<int, ServerMessageHandlerInfo> channelToHandlerDict)
        {
            return s_objectIdToChannelToServerMsgHandlerDict.TryGetValue(objectId, out channelToHandlerDict);
        }

        private static bool GetServerMsgObjectMapping(object obj, out int objectId, out Dictionary<int, ServerMessageHandlerInfo> channelToHandlerDict)
        {
            if (obj != null)
            {
                if (!s_objectToObjectIdDict.TryGetValue(obj, out objectId))
                {
                    objectId = 0;
                    channelToHandlerDict = null;
                    return false;
                }
            }
            else
            {
                objectId = 0;
            }
            return GetServerMsgChannelToHandlerDict(objectId, out channelToHandlerDict);
        }

        private static void AddServerMsgMessageHandlerInternal(object obj, Type serializedType, Action<IntPtr, int> casterHandler, int channel, bool addCleanBehaviour)
        {
            if (!GetServerMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
            {
                // New object registered
                AddObjectMapping(obj, out objectId);
                channelToHandlerDict = s_objectIdToChannelToServerMsgHandlerDict[objectId];
            }
            else
            {
                if (channelToHandlerDict.ContainsKey(channel))
                    throw new ArgumentOutOfRangeException("channel", $"Channel {channel} already has an associated handler!");
            }

            channelToHandlerDict.Add(channel, new ServerMessageHandlerInfo { Handler = casterHandler, ArgumentType = serializedType });

            if (addCleanBehaviour)
            {
                AddAutomaticCleanComponent(obj);
            }
        }

        private static int CheckServerMsgHandler(object obj, int channel, Type type, [CallerMemberName] string caller = "")
        {
            if (!GetServerMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
                throw new ArgumentOutOfRangeException("obj", $"{nameof(Cluster)}: No handler registered for object. You must register a handler before attempting to synchronize a message.");

            if (!channelToHandlerDict.TryGetValue(channel, out var handlerInfo))
                throw new ArgumentOutOfRangeException("channel", $"{nameof(Cluster)}: No handler registered for channel. You must register a handler before attempting to synchronize a message.");

            if (handlerInfo.ArgumentType != type)
                throw new ArgumentOutOfRangeException("type", $"{nameof(Cluster)}: Message type {type} does not match the registered handler type {handlerInfo.ArgumentType}.");

            return objectId;
        }

        private static bool GetClientMsgChannelToHandlerDict(int objectId, out Dictionary<int, ClientMessageHandlerInfo> channelToHandlerDict)
        {
            return s_objectIdToChannelToClientMsgHandlerDict.TryGetValue(objectId, out channelToHandlerDict);
        }

        private static bool GetClientMsgObjectMapping(object obj, out int objectId, out Dictionary<int, ClientMessageHandlerInfo> channelToHandlerDict)
        {
            if (obj != null)
            {
                if (!s_objectToObjectIdDict.TryGetValue(obj, out objectId))
                {
                    objectId = 0;
                    channelToHandlerDict = null;
                    return false;
                }
            }
            else
            {
                objectId = 0;
            }
            return GetClientMsgChannelToHandlerDict(objectId, out channelToHandlerDict);
        }

        private static void AddClientMessageHandlerInternal(object obj, Type serializedType, Action<int, IntPtr, int> casterHandler, int channel, bool addCleanBehaviour)
        {
            if (!GetClientMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
            {
                // New object registered
                AddObjectMapping(obj, out objectId);
                channelToHandlerDict = s_objectIdToChannelToClientMsgHandlerDict[objectId];
            }
            else
            {
                if (channelToHandlerDict.ContainsKey(channel))
                    throw new ArgumentOutOfRangeException("channel", $"Channel {channel} already has an associated handler!");
            }

            channelToHandlerDict.Add(channel, new ClientMessageHandlerInfo { Handler = casterHandler, ArgumentType = serializedType });

            if (addCleanBehaviour)
            {
                AddAutomaticCleanComponent(obj);
            }
        }

        private static int CheckClientMsgHandler(object obj, int channel, Type type, [CallerMemberName] string caller = "")
        {
            if (!GetClientMsgObjectMapping(obj, out var objectId, out var channelToHandlerDict))
                throw new ArgumentOutOfRangeException("obj", $"{nameof(Cluster)}: No handler registered for object. You must register a handler before attempting to synchronize a message.");

            if (!channelToHandlerDict.TryGetValue(channel, out var handlerInfo))
                throw new ArgumentOutOfRangeException("channel", $"{nameof(Cluster)}: No handler registered for channel. You must register a handler before attempting to synchronize a message.");

            if (handlerInfo.ArgumentType != type)
                throw new ArgumentOutOfRangeException("type", $"{nameof(Cluster)}: Message type {type} does not match the registered handler type {handlerInfo.ArgumentType}.");

            return objectId;
        }

        private static vrClusterManager CheckClusterManager([CallerMemberName] string caller = "")
        {
            var clusterMgr = MVR.ClusterMgr;

            if (clusterMgr == null)
                throw new InvalidOperationException($"{nameof(Cluster)}: Cannot call {caller} while MiddleVR is not initialized.");

            return clusterMgr;
        }
        #endregion

        #region Private : Automatic cleaning unity component
        private static void AddAutomaticCleanComponent(object obj)
        {
            // Automatically remove synchronization handlers for gameObjects
            var gameObject = (obj as MonoBehaviour)?.gameObject;
            if (gameObject != null)
            {
                var cleaner = gameObject.GetComponent<ClusterSynchronizationCleaner>();
                if (cleaner == null)
                    cleaner = gameObject.AddComponent<ClusterSynchronizationCleaner>();
                cleaner._objectsToClean.Add(obj);
            }
        }

        private class ClusterSynchronizationCleaner : MonoBehaviour
        {
            internal HashSet<object> _objectsToClean = new HashSet<object>();

            private void OnDestroy()
            {
                foreach (var obj in _objectsToClean)
                {
                    Remove(obj);
                }
            }
        }
        #endregion
    }
}
