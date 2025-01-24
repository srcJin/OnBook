using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

namespace Fusion.Addons.DataSyncHelpers
{
    public class StreamSynchedBehaviour : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [Networked]
        public int TotalDataLength { get; set; }
        public bool allowAnyClientEmission = false;

        List<PlayerRef> playersHavingReceivedRecoveryRequests = new List<PlayerRef>();
        List<PlayerRef> playersHavingConfirmedRecoveryRequests = new List<PlayerRef>();

        [System.Serializable]
        public struct Chunk
        {
            public PlayerRef source;
            public float time;
            public byte[] data;
        }
        public List<Chunk> cachedDataChunks = new List<Chunk>();
        protected int totalLocalDataLength = 0;
        const int TIME_PRECISION = 10_000;
        const float MAX_RESPONSE_TIME_TO_LATE_JOINERS = 5;

        float waitingForDataStart = 0;

        NetworkRunner runner;

        public enum Status
        {
            Normal,
            LateJoinersWaitingForData,
            LateJoinerReceivingData,
            MissingData
        }

        public Status status = Status.Normal;

        bool IsStateAuthorityStillConnected
        {
            get
            {
                foreach (var player in Runner.ActivePlayers)
                {
                    if (player == Object.StateAuthority)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        PlayerRef CurrentRecoveryProvider
        {
            get
            {
                if (playersHavingReceivedRecoveryRequests.Count == 0) return PlayerRef.None;
                return playersHavingReceivedRecoveryRequests[playersHavingReceivedRecoveryRequests.Count - 1];
            }
        }

        #region Log
        [System.Flags]
        public enum LogLevel
        {
            None = 0,
            Issue = 1,
            Relay = 4,
            Progress = 8,
            Reception = 16,
            LateJoinRecovery = 32,
            Details = 256,
            All = ~0,
        }
        public LogLevel logLevel = LogLevel.Issue;

        protected void Log(string log, LogLevel level = LogLevel.Details)
        {
            if ((level & logLevel) == 0)
            {
                return;
            }
            Debug.Log(log);
        }
        #endregion

        #region NetworkBehaviour
        public override void Spawned()
        {
            base.Spawned();
            runner = Runner;
            Runner.AddCallbacks(this);

            if (TotalDataLength != 0)
            {
                // Late joiner: might have missed streamed data before connecting
                StartRequestingMissingData();
            }
        }
        #endregion

        #region Monobehaviour
        protected virtual void OnDestroy()
        {
            if (runner) runner.RemoveCallbacks(this);
        }

        private void Update()
        {
            CheckMissingData();
        }
        #endregion

        #region Subclassables callbacks
        // Provides the 0-1 download progress of a currently received chunk of data
        protected virtual void OnDataProgress(float progress) { }
        // Called on complete reception of a chunk of data
        protected virtual void OnNewBytes(byte[] newData) { }
        // Called on complete reception of a chunk of data (remote source only)
        protected virtual void OnNewRemoteBytes(byte[] newData, PlayerRef source, float time) { }
        // Called to know the client is a late joiner, waiting for data
        protected virtual void OnLateJoinersWaitingForData() { }
        // Called if missing data for a late joiner are unavailable anywhere
        protected virtual void OnMissingData() { }
        #endregion

        #region Late joiner logic to recover missed data
        void StartRequestingMissingData()
        {
            status = Status.LateJoinersWaitingForData;
            OnLateJoinersWaitingForData();
            RequestMissingData();
        }

        void CheckMissingData()
        {
            if (status != Status.LateJoinersWaitingForData) return;

            if (waitingForDataStart != 0 && (Time.time - waitingForDataStart) < MAX_RESPONSE_TIME_TO_LATE_JOINERS)
            {
                // Still waiting for a pending request
                return;
            }
            RequestMissingData();
        }

        // Subclass can override it to have their own logic of choosing who will forward data in priority (for instance, adding randomness to avoid hitting the same fallback player all the time)
        protected virtual IEnumerable<PlayerRef> PlayerList()
        {
            return Runner.ActivePlayers;
        }

        protected virtual void RequestMissingData()
        {
            if (playersHavingReceivedRecoveryRequests.Contains(Object.StateAuthority) == false && IsStateAuthorityStillConnected)
            {
                // We first ask to state auth if still connected
                RequestMissingData(Object.StateAuthority);
            }
            else
            {
                // Otherwise, we ask to the first to a player we've not yet asked to (not the state auth, and not us)
                foreach (var player in PlayerList())
                {
                    if (playersHavingReceivedRecoveryRequests.Contains(player) == false && player != Runner.LocalPlayer)
                    {
                        RequestMissingData(player);
                        return;
                    }
                }
                // No player left to ask to
                Log($"No player left to ask to for late join data", LogLevel.LateJoinRecovery);
                status = Status.MissingData;
                OnMissingData();
            }
        }

        void RequestMissingData(PlayerRef player)
        {
            Log($"RequestMissingData to {player}", LogLevel.LateJoinRecovery);
            waitingForDataStart = Time.time;
            RpcDataRequest(Runner.LocalPlayer, target: player);
        }

        [Rpc]
        public void RpcDataRequest(PlayerRef requester, PlayerRef target)
        {
            if (target != Runner.LocalPlayer) return;
            bool canProvideData = status == Status.Normal;
            if (canProvideData)
            {
                Log("Send data to late joiners", LogLevel.LateJoinRecovery);
                SendCachedDataToPlayer(requester);
                // If the transfer is long (due to initialization syncs, ...), we warn the receiver to wait for our answer a bit longer, we do have the data
                RpcConfirmDataAvailability(Runner.LocalPlayer, requester);
            }
        }

        [Rpc]
        public void RpcConfirmDataAvailability(PlayerRef provider, PlayerRef target)
        {
            if (target != Runner.LocalPlayer) return;
            Log("Current recovery provider confirmed having the data we are looking for", LogLevel.LateJoinRecovery);
            playersHavingConfirmedRecoveryRequests.Add(provider);
        }
        #endregion

        #region Reliable data transmission
        ReliableKey GenerateReliableKey()
        {
            ReliableKey key = ReliableKey.FromInts((int)TargetId(), Runner.LocalPlayer.RawEncoded, (int)Time.time, (int)(TIME_PRECISION * (Time.time - (int)Time.time)));
            return key;
        }

        (int, PlayerRef, float) ParseKey(ReliableKey key)
        {
            key.GetInts(out var key0, out var key1, out var key2, out var key3);
            var objectId = key0;
            var source = PlayerRef.FromEncoded(key1);
            var time = (float)key2 + (float)key3 / (float)TIME_PRECISION;
            return (objectId, source, time);
        }

        public virtual void Send(byte[] data)
        {
            bool hasRelevantAuthority = (Runner.Topology == Topologies.Shared && Object.HasStateAuthority == false) || (Runner.Topology == Topologies.ClientServer && Object.HasInputAuthority == false);
            if (allowAnyClientEmission == false && hasRelevantAuthority)
            {
                Debug.LogError("Data cannot be asssured to be in proper order if several players can push data to it. Rejecting send request");
                return;
            }

            ReliableKey key = GenerateReliableKey();
            Log($"[{Time.time}] Sending data (local player:{Runner.LocalPlayer}): {ByteArrayTools.PreviewString(data)}");

            // Directly stores the data localy
            OnDataChunkReceived(data, Runner.LocalPlayer, Time.time);

            if (Runner.Topology == Topologies.Shared || Runner.IsServer)
            {
                // Directly send to clients
                foreach (var player in Runner.ActivePlayers)
                {
                    if (player == Runner.LocalPlayer) continue;
                    Log($" => to {player}{((player == Runner.LocalPlayer) ? " (themselves)" : "")}", LogLevel.Details);
                    Runner.SendReliableDataToPlayer(player, key, data);
                }
            }
            else
            {
                Runner.SendReliableDataToServer(key, data); ;
            }
        }

        void SendCachedDataToPlayer(PlayerRef player)
        {
            if (totalLocalDataLength == 0) return;

            var data = new byte[totalLocalDataLength];
            int cursor = 0;
            foreach (var chunk in cachedDataChunks)
            {
                System.Buffer.BlockCopy(chunk.data, 0, data, cursor, chunk.data.Length);
                cursor += chunk.data.Length;
            }
            ReliableKey key = GenerateReliableKey();
            Runner.SendReliableDataToPlayer(player, key, data);
        }

        // Add local data, without transfering it
        public void AddLocalData(byte[] data)
        {
            AddLocalData(data, Runner.LocalPlayer, Time.time);
        }

        // Add local data, without transfering it
        public void AddLocalData(byte[] data, PlayerRef source, float time)
        {
            cachedDataChunks.Add(new Chunk { source = source, time = time, data = data });
            totalLocalDataLength += data.Length;
            if (Object.HasStateAuthority)
            {
                TotalDataLength = totalLocalDataLength;
            }
        }

        public void ClearData(bool allowNonStateAuthorityClear = false)
        {
            if(Object && Object.HasStateAuthority == false && allowNonStateAuthorityClear == false)
            {
                throw new Exception("Not allowed to clear the data: not the state auth");
            }
            // We cannot rely on detecting a TotalDataLength set to 0 by the state auth, has it could have alerady filled it again when we receive the value
            RpcClearData();
        }

        protected virtual void OnDataCleared()
        {

        }


        [Rpc]
        public void RpcClearData()
        {
            LocalClearData();
        }

        public void LocalClearData()
        {
            cachedDataChunks.Clear();
            playersHavingReceivedRecoveryRequests.Clear();
            playersHavingConfirmedRecoveryRequests.Clear();
            totalLocalDataLength = 0;
            if (Object.HasStateAuthority)
            {
                TotalDataLength = 0;
            }
            OnDataCleared();
        }

        public void InsertLocalDataAtStart(byte[] data, PlayerRef source, float time)
        {
            cachedDataChunks.Insert(0, new Chunk { source = source, time = time, data = data });
            totalLocalDataLength += data.Length;
        }

        public virtual void OnDataChunkReceived(byte[] data, PlayerRef source, float time)
        {
            Log($"Added data chunk from {source} at {time}: {ByteArrayTools.PreviewString(data)}", LogLevel.Reception);
            AddLocalData(data, source, time);
            OnNewBytes(data);
            if(source != Runner.LocalPlayer)
            {
                OnNewRemoteBytes(data, source, time);
            }
        }

        #endregion

        #region INetworkRunnerCallbacks callbacks
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            if (Object == null)
            {
                Log($"Object destroyed while receiving OnReliableDataProgress", LogLevel.Issue);
                return;
            }
            (var objectId, var source, var time) = ParseKey(key);
            if (IsReliableDataTarget(TargetId()))
            {
                Log($"([{objectId}, {source}, {time}] OnReliableDataProgress progress:{progress} (local player:{Runner.LocalPlayer}) )", LogLevel.Progress);
                if (status == Status.LateJoinersWaitingForData)
                {
                    status = Status.LateJoinerReceivingData;
                }
                OnDataProgress(progress);
            }
            else
            {
                if (Runner.Topology == Topologies.Shared && source == CurrentRecoveryProvider)
                {
                    if (status == Status.LateJoinersWaitingForData && playersHavingConfirmedRecoveryRequests.Contains(source))
                    {
                        Log("Current recovery provider is still connected, confirmed having the data, but is busy sending data for other objects. We make sure to wait a bit longer", LogLevel.LateJoinRecovery);
                        waitingForDataStart = Time.time;
                    }
                }

                Log($"([{objectId}, {source}, {time}] Ignoring OnReliableDataProgress: {(int)Object.Id.Raw}/{TargetId()})", LogLevel.Details);
            }
        }

        protected virtual bool IsReliableDataTarget(int targetId)
        {
            return targetId == TargetId();
        }

        protected virtual int TargetId()
        {
            if (StreamingAPIConfiguration.HandleMultipleStreamingAPIComponentCollisions)
            {
                return Id.GetHashCode();
            }
            else
            {
                return (int)Object.Id.Raw;
            }
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef targetPlayer, ReliableKey key, ArraySegment<byte> receivedData)
        {
            if (Object == null)
            {
                Log($"Object destroyed while receiving OnReliableDataReceived", LogLevel.Issue);
                return;
            }
            (var targetId, var source, var time) = ParseKey(key);
            if (IsReliableDataTarget(targetId))
            {
                if (Runner.IsServer)
                {
                    // Received a message as the server: we relay it
                    Log($"[{targetId}, {source}, {time}] Server received OnReliableDataReceived, relaying (local player:{Runner.LocalPlayer}): {ByteArrayTools.PreviewString(receivedData.Array)}", LogLevel.Relay);
                    foreach (var player in Runner.ActivePlayers)
                    {
                        // We do not send the data back to the sending player
                        if (player == source) continue;

                        Log($" => to {player}{((player == Runner.LocalPlayer) ? " (themselves)" : "")}", LogLevel.Details);
                        if (player == Runner.LocalPlayer)
                        {
                            // For the host, store directly the data chunk locally
                            OnDataChunkReceived(receivedData.Array, source, time);
                        }
                        else
                        {
                            Runner.SendReliableDataToPlayer(player, key, receivedData.Array); ;
                        }
                    }
                }
                else
                {
                    Log($"[{targetId}, {source}, {time}] OnReliableDataReceived (local player:{Runner.LocalPlayer}): {ByteArrayTools.PreviewString(receivedData.Array)}");
                    if (status == Status.LateJoinersWaitingForData || status == Status.LateJoinerReceivingData)
                    {
                        status = Status.Normal;
                    }
                    OnDataChunkReceived(receivedData.Array, source, time);
                }
            }
            else
            {
                Log($"([{targetId}, {source}, {time}] Ignoring OnReliableDataReceived {(int)Object.Id.Raw} / {TargetId()})", LogLevel.Details);
            }
        }

        #endregion

        #region Unused INetworkRunnerCallbacks callbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        #endregion

        #region Data parsing
        protected T[] SplitCompleteData<T>() where T : unmanaged, IByteArraySerializable
        {
            var data = new byte[totalLocalDataLength];
            int cursor = 0;
            foreach (var chunk in cachedDataChunks)
            {
                System.Buffer.BlockCopy(chunk.data, 0, data, cursor, chunk.data.Length);
                cursor += chunk.data.Length;
            }

            ByteArrayTools.Split<T>(data, out var newPaddingStartBytes, out var entries);

            return entries;
        }
        #endregion
    }

}
