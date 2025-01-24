using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Fusion.Addons.DataSyncHelpers
{
    /*
     * Ring buffer behaviour, that synch the latest entries stored in the buffer (converted as bytes), as well as the total amount of entries added.
     * They are not accessible for late joiners: for this feature, look at the RingBufferLossLessSyncBehaviour subclass
     * 
     * It relies on the RingBuffer struct, which stores the ring buffer index positions, the total amount of data, and manages the changes when added data or receiving a new state of the buffer data source
     */
    public class RingBufferSyncBehaviour<TEntry> : NetworkBehaviour, RingBuffer.IRingBufferDataSource where TEntry : unmanaged, RingBuffer.IRingBufferEntry
    {
        public const int BUFFER_SIZE = 1000;
        [Networked, Capacity(BUFFER_SIZE)]
        public NetworkArray<byte> Data { get; }

        public RingBuffer ringBuffer;
        ChangeDetector changeDetector;

        int _entrySize = 0;
        public int EntrySize { 
            get
            {
                if(_entrySize == 0)
                {
                    _entrySize = ByteArrayTools.ByteArrayRepresentationLength<TEntry>();
                }
                return _entrySize;
            }
        }
        public int AddedEntryCount => ringBuffer.positionInfo.totalData / EntrySize;

        public ChangeDetector.Source changeDetectionSource = ChangeDetector.Source.SimulationState;
        #region Log
        [System.Flags]
        public enum LogLevel
        {
            None = 0,
            Issue = 1,
            Details = 2,
            Test = 4,
            DataLossRecovery = 8,
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

        protected int maxRingBufferEntries = 0;

        protected unsafe void DetermineMaxCache()
        {
            maxRingBufferEntries = (BUFFER_SIZE - RingBuffer.HEADER_LENGTH) / EntrySize;
        }

        public override void Spawned()
        {
            base.Spawned();
            DetermineMaxCache();
            ringBuffer = new RingBuffer(Data.Length);
            if (Object.HasStateAuthority)
            {
                ringBuffer.SaveBufferHeaderToDatasource(this);
            }
            changeDetector = GetChangeDetector(changeDetectionSource);
            CheckDataChange();
        }

        public override void Render()
        {
            base.Render();
            foreach (var changedVar in changeDetector.DetectChanges(this))
            {
                if (changedVar == nameof(Data))
                {
                    CheckDataChange();
                }
            }
        }

        #region RingBuffer.IRingBufferDataSource
        public int DataLength => Data.Length;
        public byte DataAt(int index)
        {
            return Data[index];
        }

        public void SetDataAt(int index, byte value)
        {
            Data.Set(index, value);
        }
        #endregion

        // Compare the change in the data array that are new for the ring buffer
        // Note: the host won't detect the changes it made, as its ringBuffer is already storing the updated indexes after the changes
        void CheckDataChange()
        {
            var change = UpdateRingBuffer(out var newData);

            if (newData.Length > 0)
            {
                OnNewBytes(newData);
            }
        }

        protected virtual RingBuffer.Change UpdateRingBuffer(out byte[] newData)
        {
            var change = ringBuffer.DataUpdate(this, out newData);
            if (newData.Length > 0) Log($"Data change {newData.Length}. Loss = {change.lossRange.Count} {((change.lossRange.Count == 0) ? "" : $"({change.lossRange.start} - {change.lossRange.end})")}", LogLevel.Details);
            if (change.lossRange.Count > 0)
            {
                OnDataloss(change.lossRange);
            }
            return change;
        }


        public virtual void AddData(byte[] newData)
        {
            if (Object.HasStateAuthority == false)
            {
                throw new System.Exception("Unable to add data: not state auth");
            }
            ringBuffer.AddData(this, newData);
        }

        public virtual void AddEntry(TEntry entry)
        {
            if (Object.HasStateAuthority == false)
            {
                throw new System.Exception("Unable to add entry: not state auth");
            }

            ringBuffer.AddEntry(this, entry);
        }

        public virtual void OnNewBytes(byte[] newData)
        {
            ringBuffer.Split<TEntry>(newData, out var newPaddingStartBytes, out var newEntries, out var entrySize);

            var positionInfo = RingBuffer.PositionInfo.ExtractPositionInfo(this);
            var totalBytes = positionInfo.totalData;
            var totalEntryCount = totalBytes / entrySize;
            int firstEntryIndex = totalEntryCount - newEntries.Length;

            Log($"New data: {newData.Length} bytes, Entries: {newEntries.Length} (entry first index: {firstEntryIndex}), Non entry bytes: {newPaddingStartBytes.Length}, {ByteArrayTools.PreviewString(newData)}", LogLevel.Details);
            OnNewEntries(newPaddingStartBytes, newEntries, firstEntryIndex);
        }

        #region Subclassables callbacks
        // OnDataLoss will warn of data loss: no need to handle the loss, the request are sent automatically
        public virtual void OnDataloss(RingBuffer.LossRange lossRange) { }

        /*
         * newEntries will containt the new TEntry detected in the last data update.
         * The received data at start might not be long enough to form an entry (for late joiners first ring reception, ...), 
         *  those additional bytes are stored in newPaddingStartBytes
         */
        public virtual void OnNewEntries(byte[] newPaddingStartBytes, TEntry[] newEntries, int firstEntryIndex) {
            OnNewEntries(newPaddingStartBytes, newEntries);
        }

        public virtual void OnNewEntries(byte[] newPaddingStartBytes, TEntry[] newEntries) { }
        #endregion

        #region Interpolation
        //Find the entry count interpolated between each interpolation state entry count (from and to)
        public bool TryGetInterpolationTotalEntryCount(out int count)
        {
            count = 0;
            if (TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha))
            {
                var reader = GetArrayReader<byte>(nameof(Data));
                var toData = reader.Read(toBuffer);
                var fromData = reader.Read(fromBuffer);
                RingBuffer.PositionInfo fromPositionInfo = RingBuffer.PositionInfo.ExtractPositionInfo(fromData);
                var fromGlobalByteIndex = fromPositionInfo.totalData - 1;
                RingBuffer.PositionInfo toPositionInfo = RingBuffer.PositionInfo.ExtractPositionInfo(toData);
                var toGlobalByteIndex = toPositionInfo.totalData - 1;
                var fromGlobalIndex = RingBuffer.EntryIndexAtDataSourcePosition<TEntry>(fromGlobalByteIndex);
                var toGlobalIndex = RingBuffer.EntryIndexAtDataSourcePosition<TEntry>(toGlobalByteIndex);

                count = (int)Mathf.Lerp(fromGlobalIndex, toGlobalIndex, alpha);
                return true;
            }
            return false;
        }
        #endregion
    }

}
