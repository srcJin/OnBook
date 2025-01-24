using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

// Simple class to test SendReliableData
public class TestSendReliableData : NetworkBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    int randomDataSizeToSend = 10_000;
    [SerializeField]
    byte[] dataToSend;
    [SerializeField]
    byte[] receivedData;
    [SerializeField]
    bool sendToLocalPlayer = false;
    [SerializeField]
    bool displayDetailedLog = false;

    public static void FillWithRandomInt(ref byte[] data, int size, byte rangeMin = 0, byte rangeMax = 100)
    {
        if (data != null && data.Length != size)
        {
            data = new byte[size];
        }
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)UnityEngine.Random.Range(rangeMin, rangeMax);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        Runner.AddCallbacks(this);
    }

    [EditorButton("Send")]
    public void Send()
    {
        // Fill data with random bytes
        FillWithRandomInt(ref dataToSend, randomDataSizeToSend);
        Send(dataToSend);
    }

    public void Send(byte[] data)
    {
        // Generate random key 
        ReliableKey key = ReliableKey.FromInts((int)Object.Id.Raw, (int)Time.time, UnityEngine.Random.Range(0, 1000), 0);
        Debug.Log($"Sending data (local player:{Runner.LocalPlayer}): {data.Length} bytes");

        foreach(var player in Runner.ActivePlayers)
        {
            if (sendToLocalPlayer == false && player == Runner.LocalPlayer) continue;
            Debug.Log($" => to {player}{((player == Runner.LocalPlayer)?" (themselves)":"")}");
            Runner.SendReliableDataToPlayer(player, key, data);  ;
        }
    }

    #region INetworkRunnerCallbacks callbacks
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        if (displayDetailedLog)
        {
            key.GetInts(out var key0, out var key1, out var key2, out var key3);
            if (key0 == (int)Object.Id.Raw)
            {
                Debug.Log($"(OnReliableDataProgress progress:{progress} player:{player} key: {key} (local player:{Runner.LocalPlayer}) )");
            }
            else
            {
                Debug.Log($"(Ignoring OnReliableDataProgress: player:{player}, key ({key0},{key1},{key2},{key3}) / {(int)Object.Id.Raw})");
            }
        }
    }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> receivedSegment)
    {
        key.GetInts(out var key0, out var key1, out var key2, out var key3);
        if (key0 == (int)Object.Id.Raw)
        {
            var data = receivedSegment.Array;
            if (receivedData == null || receivedData.Length != data.Length)
            {
                receivedData = new byte[data.Length]; ;
            }
            data.CopyTo(receivedData, 0);
            Debug.Log($"OnReliableDataReceived player:{player}, key: ({key0},{key1},{key2},{key3}) (local player:{Runner.LocalPlayer}): {receivedData.Length}");
        }
        else
        {
            Debug.Log($"(Ignoring OnReliableDataReceived: player:{player}, key ({key0},{key1},{key2},{key3}) / {(int)Object.Id.Raw})");
        }
    }
    #endregion

    #region Unused INetworkRunnerCallbacks callbacks
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
    #endregion
}
