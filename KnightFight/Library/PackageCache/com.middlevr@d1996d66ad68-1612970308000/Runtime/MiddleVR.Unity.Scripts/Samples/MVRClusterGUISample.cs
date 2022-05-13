using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiddleVR;
using MiddleVR.Unity;

public class MVRClusterGUISample : MonoBehaviour
{
    void Start()
    {
        Cluster.AddMessageHandler<bool>(this, ClusterOnButtonPress, channel: 1);
    }

    public void OnButtonPress()
    {
        Debug.Log("OnButtonPress");
        Cluster.BroadcastMessage(this, true, channel: 1);
    }

    private void ClusterOnButtonPress(bool iBool)
    {
        // Actually open door on all cluster nodes

        // Note: This will also be called even if the configuration
        // is not a cluster configuration

        Debug.Log("ClusterOnButtonPress");
        Debug.Log("OpenDoor!");
    }
}
