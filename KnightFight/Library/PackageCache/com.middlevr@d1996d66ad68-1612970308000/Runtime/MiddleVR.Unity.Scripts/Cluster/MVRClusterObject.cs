/* MVRClusterObject
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections.Generic;
using System.IO;
using MiddleVR.Unity;
using UnityEngine;

/// <summary>
/// Synchronize a GameObject or a GameObject hierarchy across the cluster
/// </summary>
[AddComponentMenu("MiddleVR/Cluster/Cluster Object")]
public class MVRClusterObject : MonoBehaviour
{
    #region Public API
    [Tooltip("Synchronize the local position, rotation and scale of this GameObject")]
    public bool SynchronizeTransforms = true;

    /// <summary>
    /// Synchronizing MonoBehaviours requires that:
    /// - Properties can be serializable
    /// - MonoBehaviours are added in the same order and at the same time
    ///   on every cluster node.
    /// </summary>
    [Tooltip("Synchronize the serializable members of this GameObject's MonoBehaviours")]
    public bool SynchronizeMonoBehaviours = false;

    [Tooltip("Also synchronize all children of this GameObject")]
    public bool SynchronizeChildren = false;

    /// <summary>
    /// Takes a snapshot of all transforms/behaviour on the GameObject or the GameObject's hierarchy.
    /// </summary>
    public void Refresh()
    {
        if (SynchronizeTransforms)
        {
            if (SynchronizeChildren)
            {
                GetComponentsInChildren(true, _transforms);
            }
            else
            {
                GetComponents(_transforms);
            }
        }

        if (SynchronizeMonoBehaviours)
        {
            if (SynchronizeChildren)
            {
                GetComponentsInChildren(true, _behaviours);
            }
            else
            {
                GetComponents(_behaviours);
            }
        }
    }
    #endregion

    #region Private members and types
    private readonly List<Transform> _transforms = new List<Transform>();
    private readonly List<MonoBehaviour> _behaviours = new List<MonoBehaviour>();

    private enum Messages
    {
        SyncTransforms,
        SyncBehaviours
    }

    private struct SerializedTransform
    {
        public SerializedTransform(Transform t)
        {
            Position = t.localPosition;
            Rotation = t.localRotation;
            Scale = t.localScale;
        }

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
    #endregion

    #region MonoBehaviour Lifetime
    private void Awake()
    {
        Cluster.AddMessageHandler(this, OnSyncTransform, Messages.SyncTransforms, false);
        Cluster.AddMessageHandler(this, OnSyncBehaviours, Messages.SyncBehaviours, false);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        _transforms.Clear();
        _behaviours.Clear();
    }

    private void OnDestroy()
    {
        Cluster.Remove(this);
    }
    #endregion

    #region Cluster object synchronization
    private static readonly MemoryStream s_writerMemoryStream = new MemoryStream();
    private static readonly BinaryWriter s_writer = new BinaryWriter(s_writerMemoryStream);

    private static void ResetWriterMemoryStream()
    {
        s_writerMemoryStream.Seek(0, SeekOrigin.Begin);
        s_writerMemoryStream.SetLength(0);
    }

    private void Update()
    {
        if (!Cluster.IsServer)
            return;

#if !ENABLE_IL2CPP
        if (_transforms.Count > 0)
        {
            ResetWriterMemoryStream();

            s_writer.Write((int)_transforms.Count);
            foreach (var transform in _transforms)
            {
                var serializedTransform = new SerializedTransform(transform);
                MiddleVR.Unity.Serialization.SerializationUtil.Write(s_writer, ref serializedTransform);
            }

            Cluster.BroadcastMessage(this, s_writerMemoryStream, Messages.SyncTransforms);
        }

        if (_behaviours.Count > 0)
        {
            ResetWriterMemoryStream();

            s_writer.Write((int)_behaviours.Count);
            foreach (var behaviour in _behaviours)
            {
                var referencableBehaviour = behaviour;
                MiddleVR.Unity.Serialization.SerializationUtil.Write(s_writer, ref referencableBehaviour);
            }

            Cluster.BroadcastMessage(this, s_writerMemoryStream, Messages.SyncBehaviours);
        }
#endif
    }

    private void OnSyncTransform(BinaryReader reader)
    {
#if !ENABLE_IL2CPP
        if (!Cluster.IsClient)
            return;

        int transformCount = reader.ReadInt32();
        for (int i = 0; i < transformCount; ++i)
        {
            var serializedTransform = default(SerializedTransform);
            MiddleVR.Unity.Serialization.SerializationUtil.Read(reader, ref serializedTransform);

            var transform = _transforms[i];
            transform.localPosition = serializedTransform.Position;
            transform.localRotation = serializedTransform.Rotation;
            transform.localScale = serializedTransform.Scale;
        }
#endif
    }

    private void OnSyncBehaviours(BinaryReader reader)
    {
#if !ENABLE_IL2CPP
        if (!Cluster.IsClient)
            return;

        int behaviourCount = reader.ReadInt32();
        for (int i = 0; i < behaviourCount; ++i)
        {
            var referencableBehaviour = _behaviours[i];
            MiddleVR.Unity.Serialization.SerializationUtil.Read(reader, ref referencableBehaviour);
        }
#endif
    }
    #endregion
}
