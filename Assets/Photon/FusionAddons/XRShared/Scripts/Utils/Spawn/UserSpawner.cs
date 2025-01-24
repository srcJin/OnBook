using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared
{
    // Very simple user prefab spawner, that cna be use with FusionBootstrap.
    // Alternatively, the ConnectionManager add-on and component can also handle this, while also managing the toplogy selection and game start.
    public class UserSpawner : MonoBehaviour, IUserSpawner, INetworkRunnerCallbacks
    {
        [Header("Fusion settings")]
        [Tooltip("Fusion runner. Automatically created if not set, on the object, or in the scene")]
        public NetworkRunner runner;

        [Header("Local user spawner")]
        public NetworkObject userPrefab;

        #region IUserSpawner
        public NetworkObject UserPrefab
        {
            get => userPrefab;
            set => userPrefab = value;
        }
        #endregion


        // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option)
        private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();

        private void Awake()
        {
            // Check if a runner exist on the same game object
            if (runner == null) runner = GetComponent<NetworkRunner>();

            // Check if a runner exist in the scene
            if (runner == null) runner = FindObjectOfType<NetworkRunner>();

            // Create the Fusion runner and let it know that we will be providing user input
            if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();
        }

        private void Start()
        {
            if (runner && new List<NetworkRunner>(GetComponentsInParent<NetworkRunner>()).Contains(runner) == false)
            {
                // The UserSpawner is not in the hierarchy of the runner, so it has not been automatically subscribed to its callbacks
                runner.AddCallbacks(this);
            }
        }

        #region Player spawn
        public void OnPlayerJoinedSharedMode(NetworkRunner runner, PlayerRef player)
        {
            if (player == runner.LocalPlayer && userPrefab != null)
            {
                // Spawn the user prefab for the local user
                NetworkObject networkPlayerObject = runner.Spawn(userPrefab, position: transform.position, rotation: transform.rotation, player, (runner, obj) => {
                });
            }
        }

        public void OnPlayerJoinedHostMode(NetworkRunner runner, PlayerRef player)
        {
            // The user's prefab has to be spawned by the host
            if (runner.IsServer && userPrefab != null)
            {
                Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");
                // We make sure to give the input authority to the connecting player for their user's object
                NetworkObject networkPlayerObject = runner.Spawn(userPrefab, position: transform.position, rotation: transform.rotation, inputAuthority: player, (runner, obj) => {
                });

                // Keep track of the player avatars so we can remove it when they disconnect
                _spawnedUsers.Add(player, networkPlayerObject);
            }
        }

        // Despawn the user object upon disconnection
        public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
        {
            // Find and remove the players avatar (only the host would have stored the spawned game object)
            if (_spawnedUsers.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedUsers.Remove(player);
            }
        }

        #endregion

        #region INetworkRunnerCallbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.Topology == Topologies.ClientServer)
            {
                OnPlayerJoinedHostMode(runner, player);
            }
            else
            {
                OnPlayerJoinedSharedMode(runner, player);
            }
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.Topology == Topologies.ClientServer)
            {
                OnPlayerLeftHostMode(runner, player);
            }
        }
        #endregion

        #region INetworkRunnerCallbacks (debug log only)
        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("OnConnectedToServer");

        }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log("Shutdown: " + shutdownReason);
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason)
        {
            Debug.Log("OnDisconnectedFromServer: " + reason);
        }
        public void OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason)
        {
            Debug.Log("OnConnectFailed: " + reason);
        }
        #endregion

        #region Unused INetworkRunnerCallbacks 

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey reliableKey, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey reliableKey, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        #endregion    
    }

}
