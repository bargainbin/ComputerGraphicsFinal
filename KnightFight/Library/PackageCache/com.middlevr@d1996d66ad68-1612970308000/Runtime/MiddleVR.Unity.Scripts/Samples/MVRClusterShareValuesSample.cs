/* MVRClusterShareValuesSample
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.Collections.Generic;
using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

public class MVRClusterShareValuesSample : MonoBehaviour
{
    #region MonoBehaviour
    private void Start()
    {
        Cluster.AddMessageHandler<float>(this, OnSynchronizedFloat, channel: 1);
        Cluster.AddMessageHandler<Vector3>(this, OnSynchronizedVector3, channel: 2);
        Cluster.AddMessageHandler<SynchronizedStruct>(this, OnSynchronizedStruct, channel: 3);
        Cluster.AddMessageHandler(this, OnSynchronizedByteArray, channel: 4);
        Cluster.AddMessageHandler(this, OnSynchronizedString, channel: 5);
    }

    // On the server, synchronize a SynchronizedState every update
    // On all nodes, OnSynchronizedState will be called the next time there is a synchronization update :
    // either during VRManagerScript.Update() or VRManagerPostFrame.Update() (see script ordering)
    private void Update()
    {
        MVRTools.Log(2, $"[ ] Frame {MVR.Kernel.GetFrame()}");

        BroadcastFloat();
        BroadcastVector3();
        BroadcastStruct();
        BroadcastByteArray();
        BroadcastString();
    }
    #endregion

    #region SampleSynchronizeFloat
    public float SyncFloat = 0.0f;

    private void BroadcastFloat()
    {
        if (Cluster.IsServer)
        {
            SyncFloat = UnityEngine.Random.Range(0, 1000);
            MVRTools.Log(2, $"[ ] Server setting float to: {SyncFloat}");
            Cluster.BroadcastMessage(this, SyncFloat, channel: 1);
        }
    }

    private void OnSynchronizedFloat(float iFloat)
    {
        if (Cluster.IsServer)
            return;

        SyncFloat = iFloat;
        MVRTools.Log(2, $"[ ] Client getting float: {SyncFloat}");
    }
    #endregion

    #region SampleSynchronizeVector3
    public Vector3 SyncVector3 = new Vector3(0.0f, 0.0f, 0.0f);

    private void BroadcastVector3()
    {
        if (Cluster.IsServer)
        {
            SyncVector3 = UnityEngine.Random.insideUnitSphere;
            MVRTools.Log(2, $"[ ] Server setting vector3 to: {SyncVector3}");
            Cluster.BroadcastMessage(this, SyncVector3, channel: 2);
        }
    }

    private void OnSynchronizedVector3(Vector3 iVec)
    {
        if (Cluster.IsServer)
            return;

        SyncVector3 = iVec;
        MVRTools.Log(2, $"[ ] Client getting vector3: {SyncVector3}");
    }
    #endregion

    #region SampleSynchronizeStruct
    private struct SynchronizedStruct
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private void BroadcastStruct()
    {
        if (Cluster.IsServer)
        {
            SynchronizedStruct syncStruct = new SynchronizedStruct
            {
                Position = transform.position,
                Rotation = transform.rotation
            };

            MVRTools.Log(2, $"[ ] Server setting struct to: pos={syncStruct.Position}, rot={syncStruct.Rotation}");

            Cluster.BroadcastMessage(this, syncStruct, channel: 3);
        }
    }

    private void OnSynchronizedStruct(SynchronizedStruct iSyncStruct)
    {
        if (Cluster.IsServer)
            return;

        transform.SetPositionAndRotation(iSyncStruct.Position, iSyncStruct.Rotation);

        MVRTools.Log(2, $"[ ] Client getting struct: pos={iSyncStruct.Position}, rot={iSyncStruct.Rotation}");
    }
    #endregion

    #region SampleSynchronizeByteArray
    public byte[] SyncArray = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

    private void BroadcastByteArray()
    {
        if (Cluster.IsServer)
        {
            for (int i = 0; i < SyncArray.Length; i++)
            {
                SyncArray[i] = (byte)UnityEngine.Random.Range(0, 0x0F);
            }

            MVRTools.Log(2, $"[ ] Server setting array to: {BitConverter.ToString(SyncArray)}");
            Cluster.BroadcastMessage(this, SyncArray, channel: 4);
        }
    }

    private void OnSynchronizedByteArray(byte[] iArray)
    {
        if (Cluster.IsServer)
            return;

        SyncArray = iArray;
        MVRTools.Log(2, $"[ ] Client getting byte array {BitConverter.ToString(SyncArray)}.");
    }
    #endregion

    #region SampleSynchronizeString
    public string SyncString;

    private void BroadcastString()
    {
        if (Cluster.IsServer)
        {
            // Randomly choose a string
            var list = new List<string> { "Buddy you're a boy", "make a big noise", "playin' in the street", "gonna be a big man some day" };
            var rnd = new System.Random();

            SyncString = list[rnd.Next(0, list.Count)];
            MVRTools.Log(2, $"[ ] Server setting string to: {SyncString}");
            Cluster.BroadcastMessage(this, SyncString, channel: 5);
        }
    }

    private void OnSynchronizedString(string iString)
    {
        if (Cluster.IsServer)
            return;

        SyncString = iString;
        MVRTools.Log(2, $"[ ] Client getting string: {SyncString}");
    }
    #endregion
}
