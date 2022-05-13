/* MVRShareTransform
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR.Unity;
using UnityEngine;

// Share a GameObject transformation using MiddleVR Cluster Synchronization.
// For more information, refer to the MiddleVR User Guide
public class MVRShareTransform : MonoBehaviour
{
    private struct SynchronizedState
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private void OnSynchronizedState(SynchronizedState state)
    {
        if (Cluster.IsServer)
            return;

        transform.SetPositionAndRotation(state.Position, state.Rotation);
    }

    #region MonoBehaviour
    private void Start()
    {
        Cluster.AddMessageHandler<SynchronizedState>(this, OnSynchronizedState);
    }

    // On the server, synchronize a SynchronizedState every update
    // On all nodes, OnSynchronizedState will be called the next time there is a synchronization update :
    // either during VRManagerScript.Update() or VRManagerPostFrame.Update() (see script ordering)
    private void Update()
    {
        if (Cluster.IsServer)
        {
            Cluster.BroadcastMessage(this,
                new SynchronizedState
                {
                    Position = transform.position,
                    Rotation = transform.rotation
                });
        }
    }
    #endregion
}
