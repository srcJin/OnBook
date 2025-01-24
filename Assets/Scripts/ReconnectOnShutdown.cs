using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using Fusion.XR.Shared.Rig;


/***
 * 
 * The ReconnectOnShutdown class handles the application's behavior when the network session is shut down. 
 * To do this, it implements the INetworkRunnerCallbacks interface to respond to network events and specifically listens for the shutdown event.
 * When the network session ends, it logs the shutdown reason and relaunch the connection after a wait time (and after displaying a reconnection UI).
 * 
 ***/

public class ReconnectOnShutdown : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] GameObject disconnectPanelPrefab;
    [SerializeField] int waitDelay = 2;

    #region INetworkRunnerCallbacks
    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Connection shutdown: {shutdownReason}.");
        var rig = FindObjectOfType<HardwareRig>();
        if (disconnectPanelPrefab && rig) {
            // Wait for the headset to be repositionned post focus
            await System.Threading.Tasks.Task.Delay(1_000);
            if (rig) {
                var panelPosition = rig.headset.transform.position + rig.headset.transform.forward * 0.7f;
                var panelRotation = Quaternion.LookRotation(rig.headset.transform.position - panelPosition);
                panelRotation = Quaternion.Euler(0, panelRotation.eulerAngles.y, 0);
                GameObject.Instantiate(disconnectPanelPrefab, panelPosition, panelRotation);
            }
        }
        int i = waitDelay;
        while (i > 0 && Application.isPlaying) { 
            await System.Threading.Tasks.Task.Delay(1_000);
            Debug.Log($"Reconnecting in {i} ...");
            i--;
        }
        if (Application.isPlaying)
        {
            Debug.Log("Reconnection");
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
    #endregion

    #region Unused INetworkRunnerCallbacks 
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}
