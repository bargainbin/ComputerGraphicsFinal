/* MVRClusterBroadcastEventsSample
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.Collections.Generic;
using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

public class MVRClusterBroadcastEventsSample : MonoBehaviour
{
    private void Start()
    {
        // All cluster nodes will handle those messages
        Cluster.AddMessageHandler<bool>(this, OnRandomEvent, channel: 0);
        Cluster.AddMessageHandler<int>(this, OnPlayerConnected, channel: 1);
        Cluster.AddMessageHandler(this, OnDownloadCompleted, channel: 2);

        // Only the server will handle those messages
        Cluster.AddClientMessageHandler(this, OnClientMessageReceived, channel: 0);
    }

    private void Update()
    {
        // This is where you would test for a random event
        bool randomEvent = true;
        if( randomEvent ) { Cluster.BroadcastMessage(this, true, channel: 0); }

        // This is where you would test for a player connection
        bool playerConnected = true;
        int playerId = 1;
        if (playerConnected) { Cluster.BroadcastMessage(this, playerId, channel: 1); }

        // This is where you would test for a completed download and 
        bool downloadCompleted = true;
        byte[] downloadedData = new byte[1024];
        if (downloadCompleted) { Cluster.BroadcastMessage(this, downloadedData, channel: 2); }

        if (MVR.ClusterMgr.IsClient()) Cluster.SendAsyncMessageToServer(this, "Hello from '" + MVR.ClusterMgr.GetMyClusterClient().GetName() + "' !");
    }

    private void OnRandomEvent(bool iBool)
    {
        if (Cluster.IsServer)
            return;

        MVRTools.Log(2, $"[ ] Client getting random event: {iBool}");
    }

    private void OnPlayerConnected(int iId)
    {
        if (Cluster.IsServer)
            return;

        MVRTools.Log(2, $"[ ] Client getting player connection event, id: {iId}");
    }

    private void OnDownloadCompleted(byte[] iArray)
    {
        if (Cluster.IsServer)
            return;

        MVRTools.Log(2, $"[ ] Client getting byte array {BitConverter.ToString(iArray)}.");
    }

    private void OnClientMessageReceived(int clientId, string message)
    {
        MVRTools.Log(2, $"[ ] Client message {message} from client id {clientId}, clent name = {MVR.ClusterMgr.Clients[clientId].GetName()}");
    }
}
