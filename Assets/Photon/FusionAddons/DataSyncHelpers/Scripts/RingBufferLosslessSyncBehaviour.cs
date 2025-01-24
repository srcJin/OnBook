using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;


namespace Fusion.Addons.DataSyncHelpers
{
    /*
     * Historized ring buffer behaviour, that relies on a NetworkArray to synchornize the ring buffer,
     *  but can also use the streaming API to send the full list of data to the late joiners
     *  
     *  Based on RingBufferSyncBehaviour and its underlying ringBuffer struct
     */
    public class RingBufferLosslessSyncBehaviour<TEntry> : RingBufferSyncBehaviour<TEntry>, INetworkRunnerCallbacks where TEntry : unmanaged, RingBuffer.IRingBufferEntry
    {
        public byte[] completeCache = new byte[0];

        // Store data not received (ring buffer filled too rapidly, late joiners, ...)
        [System.Serializable]
        public struct LossRequest
        {
            public enum Status
            {
                Requesting,
                NoAnswer
            }
            public Status status;
            public ReliableKey requestKey;
            public RingBuffer.LossRange lossRange;
            public float progress;
            public List<PlayerRef> playersSentTo;
            public float lastRequestUpdate;

            public LossRequest(RingBuffer.LossRange lossRange, ReliableKey requestKey)
            {
                this.lossRange = lossRange;
                this.requestKey = requestKey;
                playersSentTo = new List<PlayerRef>();
                lastRequestUpdate = 0;
                progress = 0;
                status = Status.Requesting;
            }

            public bool HasAlreadySentTo(PlayerRef player)
            {
                return playersSentTo.Contains(player);
            }

            public float TimeSinceLastRequestUpdate()
            {
                return Time.time - lastRequestUpdate;
            }

            public void SentTo(PlayerRef player)
            {
                playersSentTo.Add(player);
                lastRequestUpdate = Time.time;
            }

            public void Progress(float f)
            {
                progress = f;
                lastRequestUpdate = Time.time;
            }
        }

        // Range of data missing
        public List<RingBuffer.LossRange> lossRanges = new List<RingBuffer.LossRange>();
        // requests sent to ask for the missing ranges
        public List<LossRequest> lossRequests = new List<LossRequest>();

        bool registeredToCallback = false;

        const float delayBetweenRequestAttempts = 5;

        public override void Render()
        {
            base.Render();

            CheckDataLoss();

            CheckLossRequestupdates();
        }

        public void CheckLossRequestupdates()
        {
            // Update requests        
            for (int i = 0; i < lossRequests.Count; i++)
            {
                var request = lossRequests[i];
                if (request.status == LossRequest.Status.NoAnswer)
                {
                    continue;
                }
                bool noOneLeftToAnswer = true;
                if (request.TimeSinceLastRequestUpdate() > delayBetweenRequestAttempts)
                {
                    foreach (var player in Runner.ActivePlayers)
                    {
                        if (player != Runner.LocalPlayer && request.HasAlreadySentTo(player) == false)
                        {
                            Log($"Requesting missing data. No answer from {request.playersSentTo.Count} users, requesting to {player}  [{request.lossRange.start} - {request.lossRange.end}]", LogLevel.DataLossRecovery); request.SentTo(player);
                            lossRequests[i] = request;
                            RpcRequestRange(Runner.LocalPlayer, player, request.lossRange.start, request.lossRange.end);
                            noOneLeftToAnswer = false;
                            break;
                        }
                    }
                    if (noOneLeftToAnswer)
                    {
                        request.status = LossRequest.Status.NoAnswer;
                        Debug.LogError($"No player managed to provide missing data: [{request.lossRange.start} - {request.lossRange.end}] for "+this);
                        lossRequests[i] = request;
                        OnNoAnswerForALossRequest(request);
                    }
                }
            }
        }

        protected override RingBuffer.Change UpdateRingBuffer(out byte[] newData)
        {
            var change = ringBuffer.DataUpdate(this, out newData, ref completeCache);
            if (newData.Length > 0) Log($"Data change {newData.Length}. Loss = {change.lossRange.Count} {((change.lossRange.Count == 0) ? "" : $"({change.lossRange.start} - {change.lossRange.end})")}", LogLevel.Details);
            if (change.lossRange.Count > 0)
            {
                OnDataloss(change.lossRange);
            }
            return change;
        }

        public override void OnDataloss(RingBuffer.LossRange lossRange)
        {
            lossRanges.Add(lossRange);
        }

        public override void AddData(byte[] newData)
        {
            if (Object.HasStateAuthority == false)
            {
                throw new System.Exception("Unable to add data: not state auth");
            }
            ringBuffer.AddData(this, newData, ref completeCache);
        }

        #region TEntry access
        public override void AddEntry(TEntry entry)
        {
            if (Object.HasStateAuthority == false)
            {
                throw new System.Exception("Unable to add entry: not state auth");
            }

            ringBuffer.AddEntry(this, entry, ref completeCache);
        }

        protected virtual TEntry[] SplitCompleteData()
        {
            var usefulData = new byte[ringBuffer.positionInfo.totalData];
            //The complete cache contains addition empty data (to avoid expending it all the time)
            System.Buffer.BlockCopy(completeCache, 0, usefulData, 0, usefulData.Length);
            ringBuffer.Split<TEntry>(usefulData, out var padding, out var entriesArray);
            return entriesArray;
        }
        #endregion

        #region Dataloss
        void CheckDataLoss()
        {
            foreach (var lossRange in lossRanges)
            {
                bool alreadyRequested = false;
                foreach (var currentRequest in lossRequests)
                {
                    if (currentRequest.lossRange.start == lossRange.start && currentRequest.lossRange.end == lossRange.end)
                    {
                        alreadyRequested = true;
                        break;
                    }
                }
                if (alreadyRequested == false)
                {
                    RequestMissingData(lossRange);
                }
            }
        }

        void RequestMissingData(RingBuffer.LossRange lossRange)
        {
            if (registeredToCallback == false)
            {
                registeredToCallback = true;
                Runner.AddCallbacks(this);
            }
            var lossRequest = new LossRequest(lossRange: lossRange, requestKey: GenerateRequestKey(Runner.LocalPlayer, lossRange));
            var requestedPlayer = PlayerRef.None;
            var fallbackPlayer = PlayerRef.None;
            foreach (var player in Runner.ActivePlayers)
            {
                if (player == Object.StateAuthority)
                {
                    requestedPlayer = player;
                    Log($"Requesting missing data. First to state auth {requestedPlayer} [{lossRange.start} - {lossRange.end}]", LogLevel.DataLossRecovery);
                    break;
                }
                else if (fallbackPlayer == PlayerRef.None && player != Runner.LocalPlayer)
                {
                    fallbackPlayer = player;
                }
            }

            if (requestedPlayer == PlayerRef.None && fallbackPlayer != PlayerRef.None)
            {
                requestedPlayer = fallbackPlayer;
                Log($"Requesting missing data. State auth missing, asking to first player: {requestedPlayer} [{lossRange.start} - {lossRange.end}]", LogLevel.DataLossRecovery);
            }

            if (requestedPlayer == PlayerRef.None)
            {
                Log($"Requesting missing data, but no other player to provide data [{lossRange.start} - {lossRange.end}]", LogLevel.DataLossRecovery);
                lossRequest.status = LossRequest.Status.NoAnswer;
                lossRequests.Add(lossRequest);
                OnNoAnswerForALossRequest(lossRequest);
            }
            else
            {
                lossRequest.SentTo(requestedPlayer);
                lossRequests.Add(lossRequest);
                RpcRequestRange(Runner.LocalPlayer, requestedPlayer, lossRange.start, lossRange.end);
            }
        }


        [Rpc]
        void RpcRequestRange(PlayerRef requester, PlayerRef target, int start, int end)
        {
            if (Runner.LocalPlayer != target) return;
            if (lossRanges.Count != 0)
            {
                Log($"Received request for {requester} [{start} - {end}], but we have loss to: do not respond", LogLevel.DataLossRecovery);
                return;
            }

            Log($"Received request for {requester} [{start} - {end}]", LogLevel.DataLossRecovery);
            var key = GenerateRequestKey(requester, new RingBuffer.LossRange { start = start, end = end });
            int length = end - start + 1;
            byte[] data = new byte[length];
            System.Buffer.BlockCopy(completeCache, start, data, 0, length);
            Runner.SendReliableDataToPlayer(requester, key, data);
        }

        protected virtual void ReceivedMissingData(int start, int end, byte[] receivedData)
        {
            foreach (var currentRequest in lossRequests)
            {
                if (currentRequest.lossRange.start == start && currentRequest.lossRange.end == end)
                {
                    Log($" => loss data received [{start} - {end}]", LogLevel.DataLossRecovery);
                    var expectedLength = end - start + 1;
                    if (receivedData.Length != expectedLength)
                    {
                        throw new Exception($"Unexpected data received [{start} {end}]: {receivedData.Length}/{expectedLength}");
                    }
                    System.Buffer.BlockCopy(receivedData, 0, completeCache, start, receivedData.Length);
                    // Cleanup request
                    foreach (var loss in lossRanges)
                    {
                        if (loss.start == start && loss.end == end)
                        {
                            lossRanges.Remove(loss);
                            break;
                        }
                    }
                    OnLossRestored(currentRequest, receivedData);
                    lossRequests.Remove(currentRequest);
                    if (lossRequests.Count == 0 && registeredToCallback)
                    {
                        Runner.RemoveCallbacks(this);
                        registeredToCallback = false;
                        OnNoLossRemaining();
                    }
                    break;
                }
            }
        }
        #endregion

        #region Subclassables callbacks
        // When all loss in the total data remains. SplitCompleteData() can be called to retrieve all the TEntry 
        protected virtual void OnNoLossRemaining() { }
        // Called when one loss is restored
        protected virtual void OnLossRestored(LossRequest request, byte[] receivedData) { }
        // When a loss is permanent (no one having the data is still in the room 
        public virtual void OnNoAnswerForALossRequest(LossRequest request) { }
        #endregion
      
        ReliableKey GenerateRequestKey(PlayerRef source, RingBuffer.LossRange lossRange)
        {
            ReliableKey key = ReliableKey.FromInts(TargetId(), source.RawEncoded, lossRange.start, lossRange.end);
            return key;
        }

        (int targetId, PlayerRef source, int requestStart, int requestEnd) ParseKey(ReliableKey key)
        {
            key.GetInts(out var key0, out var key1, out var key2, out var key3);
            var targetId = key0;
            var source = PlayerRef.FromEncoded(key1);
            return (targetId, source, key2, key3);
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

        #region INetworkRunnerCallbacks callbacks
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            if (Object == null)
            {
                //Object destroyed while receiving OnReliableDataProgress
                return;
            }
            (var targetId, var requester, var start, var end) = ParseKey(key);
            if (IsReliableDataTarget(targetId))
            {
                for (int i = 0; i < lossRequests.Count; i++)
                {
                    var currentRequest = lossRequests[i];
                    if (currentRequest.lossRange.start == start && currentRequest.lossRange.end == end)
                    {
                        Log($"Data loss recovery progress [{start}-{end}]: {(int)(100 * progress)}%", LogLevel.DataLossRecovery);
                        currentRequest.Progress(progress);
                        lossRequests[i] = currentRequest;
                        break;
                    }
                    i++;
                }
            }
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef targetPlayer, ReliableKey key, ArraySegment<byte> receivedData)
        {
            if (Object == null)
            {
                //Object destroyed while receiving OnReliableDataReceived
                return;
            }
            (var targetId, var requester, var start, var end) = ParseKey(key);
            if (IsReliableDataTarget(targetId))
            {
                ReceivedMissingData(start, end, receivedData.Array);
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
    }
}

