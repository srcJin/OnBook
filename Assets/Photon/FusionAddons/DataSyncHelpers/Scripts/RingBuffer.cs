using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace Fusion.Addons.DataSyncHelpers
{
    /*
     * Historized ring buffer
     * Ring Buffer with a total data count field, to be able to provide the global index (starting from the first insertions) of the data currently in the ring buffer,
     * to provide the global indexes of the range of data added
     *  and also to warn when a range of data are missing 
     *  
     *  Does not store the actual data (just the total data "received", and actual ring buffer indexes), but deals with the actual filling of the underlying data source
     */
    [System.Serializable]
    public struct RingBuffer
    {
        // Interface for a data storage class that store tha actual data that the RingBuffer describes
        public interface IRingBufferDataSource
        {
            int DataLength { get; }
            byte DataAt(int index);
            void SetDataAt(int index, byte value);
        }

        public PositionInfo positionInfo;
        // Max amount of usable data in the data source (data storage source size minus data used for the ring buffer usage, HEADER_LENGTH)
        public short indexCount;
        public const int HEADER_LENGTH = sizeof(int) + sizeof(short) * 2;
        public short LastIndex => (short)(positionInfo.nextIndex == 0 ? indexCount - 1 : positionInfo.nextIndex - 1);
        int dataStorageSize;

        const int DEFAULT_CACHE_INCREASE_STEP = 300;

        #region Underlying structs
        // Stores main ring buffer descriptors:
        // - firstIndex: position in the data source when the first ring buffer data is
        // - nextIndex: position in the data source where the next 
        // - totalData: the total amount of data that has passed through the ring buffer
        public struct PositionInfo
        {
            public int totalData;
            public short firstIndex;
            public short nextIndex;

            // Find the totalData int, firstIndex short, nextIndex short stored in the header of a stream of byte (provided by a IRingBufferDataSource)
            public static PositionInfo ExtractPositionInfo(IRingBufferDataSource dataSource)
            {
                int cursor = 0;
                var totalDataBytes = new byte[sizeof(int)];
                for (int i = 0; i < totalDataBytes.Length; i++) totalDataBytes[i] = dataSource.DataAt(i + cursor);
                var totalData = System.BitConverter.ToInt32(totalDataBytes);
                cursor += totalDataBytes.Length;
                var firstIndexBytes = new byte[sizeof(short)];
                for (int i = 0; i < firstIndexBytes.Length; i++) firstIndexBytes[i] = dataSource.DataAt(i + cursor);
                cursor += firstIndexBytes.Length;
                var firstIndex = System.BitConverter.ToInt16(firstIndexBytes);
                var nextIndexBytes = new byte[sizeof(short)];
                for (int i = 0; i < nextIndexBytes.Length; i++) nextIndexBytes[i] = dataSource.DataAt(i + cursor);
                cursor += nextIndexBytes.Length;
                var nextIndex = System.BitConverter.ToInt16(nextIndexBytes);

                if (totalData == 0)
                {
                    // Check with an empty unitialized data source
                    firstIndex = -1;
                    nextIndex = 0;
                }

                return new PositionInfo { totalData = totalData, firstIndex = firstIndex, nextIndex = nextIndex };
            }

            // Find the totalData int, firstIndex short, nextIndex short stored in the header of a stream of byte (provided by a NetworkArrayReadOnly<byte>)
            public static PositionInfo ExtractPositionInfo(NetworkArrayReadOnly<byte> data)
            {
                int cursor = 0;
                var totalDataBytes = new byte[sizeof(int)];
                for (int i = 0; i < totalDataBytes.Length; i++) totalDataBytes[i] = data[i + cursor];
                var totalData = System.BitConverter.ToInt32(totalDataBytes);
                cursor += totalDataBytes.Length;
                var firstIndexBytes = new byte[sizeof(short)];
                for (int i = 0; i < firstIndexBytes.Length; i++) firstIndexBytes[i] = data[i + cursor];
                cursor += firstIndexBytes.Length;
                var firstIndex = System.BitConverter.ToInt16(firstIndexBytes);
                var nextIndexBytes = new byte[sizeof(short)];
                for (int i = 0; i < nextIndexBytes.Length; i++) nextIndexBytes[i] = data[i + cursor];
                cursor += nextIndexBytes.Length;
                var nextIndex = System.BitConverter.ToInt16(nextIndexBytes);

                if (totalData == 0)
                {
                    // Check with an empty unitialized data source
                    firstIndex = -1;
                    nextIndex = 0;
                }

                return new PositionInfo { totalData = totalData, firstIndex = firstIndex, nextIndex = nextIndex };
            }
        }

        // Described a data loss range (a change included more data that the ring size, so some data were not "seen"), in global history indexes
        [System.Serializable]
        public struct LossRange
        {
            public int start;
            public int end;

            public int Count => (start == -1 || end == -1) ? 0 : end - start + 1;

            public static LossRange NoLoss => new LossRange { start = -1, end = -1 };

        }

        /*
         * Describe a change on the ring buffer, compared to a previous ring buffer state
         * - amount of added data
         * - range of data changed 
         * - range of loss data (the change included more data that the ring size, so some data were not "seen"), in global history indexes
         */
        [System.Serializable]
        public struct Change
        {
            [System.Serializable]
            public struct ChangeChunk
            {
                public short startIndex;
                public short endIndex;
            }

            public int length;
            public ChangeChunk[] changeChunks;
            public LossRange lossRange;

            public Change(RingBuffer previousRingBuffer, PositionInfo newRingBufferPositionInfo)
            {
                lossRange = LossRange.NoLoss;
                short lastIndex = (short)((newRingBufferPositionInfo.nextIndex - 1) > 0 ? newRingBufferPositionInfo.nextIndex - 1 : previousRingBuffer.indexCount - 1);
                if (previousRingBuffer.positionInfo.totalData == newRingBufferPositionInfo.totalData)
                {
                    // No change
                    changeChunks = new ChangeChunk[0];
                    length = 0;
                }
                else if (previousRingBuffer.positionInfo.nextIndex > lastIndex)
                {
                    // The change filled the buffer and continued at 0
                    //Debug.LogError($"The change filled the buffer and continued at 0 ({previousRingBuffer.nextIndex} > {lastIndex})");
                    changeChunks = new ChangeChunk[2];
                    changeChunks[0].startIndex = previousRingBuffer.positionInfo.nextIndex;
                    changeChunks[0].endIndex = (short)(previousRingBuffer.indexCount - 1);
                    length = previousRingBuffer.indexCount - previousRingBuffer.positionInfo.nextIndex;
                    changeChunks[1].startIndex = 0;
                    changeChunks[1].endIndex = lastIndex;
                    length += lastIndex + 1;
                }
                else
                {
                    // Change, but either less that was remaining to fill the buffer (or more than the full buffer)
                    if ((newRingBufferPositionInfo.totalData - previousRingBuffer.positionInfo.totalData) >= previousRingBuffer.indexCount)
                    {
                        // Data loss occured
                        //TODO "=" might not be a data loss, double check
                        // Filled completely
                        //Debug.LogError($"Data lost: a whole buffer has been filled during last update {totalData - previousRingBuffer.totalData - previousRingBuffer.indexCount}");
                        lossRange.start = previousRingBuffer.positionInfo.totalData;
                        lossRange.end = newRingBufferPositionInfo.totalData - 1 - previousRingBuffer.indexCount;
                        if (newRingBufferPositionInfo.firstIndex == 0)
                        {
                            //Debug.LogError("1) Filled completly, starting at 0");
                            changeChunks = new ChangeChunk[1];
                            changeChunks[0].startIndex = 0;
                            changeChunks[0].endIndex = (short)(previousRingBuffer.indexCount - 1);
                            length = previousRingBuffer.indexCount;
                        }
                        else
                        {
                            //Debug.LogError("2) Filled completly, NOT starting at 0");
                            changeChunks = new ChangeChunk[2];
                            changeChunks[0].startIndex = newRingBufferPositionInfo.firstIndex;
                            changeChunks[0].endIndex = (short)(previousRingBuffer.indexCount - 1);
                            length = previousRingBuffer.indexCount - newRingBufferPositionInfo.firstIndex;
                            changeChunks[1].startIndex = 0;
                            changeChunks[1].endIndex = lastIndex;
                            length += lastIndex + 1;
                        }

                    }
                    else
                    {
                        //Debug.LogError("Filled less that was remaining to fill the buffer");
                        changeChunks = new ChangeChunk[1];
                        changeChunks[0].startIndex = previousRingBuffer.positionInfo.nextIndex;
                        changeChunks[0].endIndex = lastIndex;
                        length = lastIndex - previousRingBuffer.positionInfo.nextIndex + 1;
                    }
                }
            }
        }
        #endregion

        public RingBuffer(int dataStorageSize)
        {
            this.dataStorageSize = dataStorageSize;
            short indexCount = (short)(dataStorageSize - HEADER_LENGTH);
            this.indexCount = indexCount;
            positionInfo.firstIndex = -1;
            positionInfo.nextIndex = 0;
            positionInfo.totalData = 0;
        }

        public static int IndexToDataSourcePosition(int index)
        {
            return index + HEADER_LENGTH;
        }

        public static int DataSourcePositionToIndex(int dataSourcePosition)
        {
            return dataSourcePosition - HEADER_LENGTH;
        }

        // Update the buffer. Return the position to update in the source data array
        public int[] AddData(int dataLength)
        {
            //Debug.LogError($"Add data: firstIndex: {firstIndex} / dataLength: {dataLength}");
            int[] dataSourcePositionsToWrite = new int[dataLength];
            if (dataLength == 0) return dataSourcePositionsToWrite;
            int positionSet = 0;
            int cursor = positionInfo.nextIndex;
            while (positionSet < dataLength)
            {
                dataSourcePositionsToWrite[positionSet] = IndexToDataSourcePosition(cursor);
                cursor = (cursor + 1) % indexCount;
                positionSet++;
            }

            // 2 initial cases: buffer never filled once (firstIndex == 0), and buffer filled once (firstIndex == nextIndex)
            bool bufferFilledOnce = positionInfo.firstIndex == positionInfo.nextIndex;
            //if (bufferFilledOnce) Debug.LogError($"Buffer was already filled once {firstIndex} == {nextIndex}");
            if (dataLength > indexCount)
            {
                Debug.LogWarning("Too much data: some data will be losed on storage");
                bufferFilledOnce = true;
            }
            if ((dataLength + positionInfo.nextIndex) >= indexCount)
            {
                //Debug.LogError($"Data will go other the max index (({dataLength} + {nextIndex}) >= {indexCount})");
                bufferFilledOnce = true;
            }
            if (positionInfo.firstIndex == -1)
            {
                //Debug.LogError("First write: setting firstIndex to 0");
                positionInfo.firstIndex = 0;
            }

            // Counters update
            positionInfo.totalData += (byte)dataLength;
            if (bufferFilledOnce)
            {
                // The buffer has already been filled once
                //Debug.LogError("The buffer has already been filled once");
                positionInfo.firstIndex = (short)((positionInfo.nextIndex + dataLength) % indexCount);
            }
            positionInfo.nextIndex = (short)((positionInfo.nextIndex + dataLength) % indexCount);
            return dataSourcePositionsToWrite;
        }

        // Update the buffer and stress which index ranges where changed
        public Change DataUpdate(PositionInfo positionInfo)
        {
            Change change = new Change(this, positionInfo);
            this.positionInfo = positionInfo;
            return change;
        }

        #region IRingBufferDataSource
        public RingBuffer(IRingBufferDataSource dataSource) : this(dataSource.DataLength) { }

        public void SaveBufferHeaderToDatasource(IRingBufferDataSource dataSource)
        {
            var headerIndex = 0;
            var totalDataBytes = System.BitConverter.GetBytes(positionInfo.totalData);
            var firstIndexBytes = System.BitConverter.GetBytes(positionInfo.firstIndex);
            var nextIndexBytes = System.BitConverter.GetBytes(positionInfo.nextIndex);
            foreach (var totalDataByte in totalDataBytes)
            {
                dataSource.SetDataAt(headerIndex, totalDataByte);
                headerIndex++;
            }
            foreach (var firstIndexByte in firstIndexBytes)
            {
                dataSource.SetDataAt(headerIndex, firstIndexByte);
                headerIndex++;
            }
            foreach (var nextIndexByte in nextIndexBytes)
            {
                dataSource.SetDataAt(headerIndex, nextIndexByte);
                headerIndex++;
            }
        }

        #region Handling of data source updates (which include, in its header, the Ringbuffer position info update
        // Udate the RingBuffer position info (based on dataSource header data)
        public Change DataUpdate(IRingBufferDataSource dataSource)
        {
            var positionInfo = PositionInfo.ExtractPositionInfo(dataSource);

            return DataUpdate(positionInfo);
        }

        // Udate the RingBuffer position info (based on dataSource header data), and stores the new data still in the buffer in the data source
        public Change DataUpdate(IRingBufferDataSource dataSource, out byte[] newBytes)
        {
            var change = DataUpdate(dataSource);
            //if(change.length > 0) Debug.LogError($"Change: {change.length} / {change.changeChunks.Length}");
            newBytes = new byte[change.length];
            int cursor = 0;
            foreach (var changeChunk in change.changeChunks)
            {
                if (changeChunk.startIndex > changeChunk.endIndex)
                {
                    throw new System.Exception("Error in change info");
                }
                //Debug.LogError($" chunk {changeChunk.startIndex}-{changeChunk.endIndex}");
                for (int i = changeChunk.startIndex; i <= changeChunk.endIndex; i++)
                {
                    //Debug.LogError($"=> change");
                    newBytes[cursor] = dataSource.DataAt(IndexToDataSourcePosition(i));
                    cursor++;
                }
            }
            return change;
        }

        // Udate the RingBuffer position info (based on dataSource header data), stores the new data still in the buffer in the data source, and stores the new data still in the buffer in the complete cache
        //  Increase the complete cache size if needed (by allocating cacheIncreaseStep bytes, which should be big enough to avoid allocating too often)
        public Change DataUpdate(IRingBufferDataSource dataSource, out byte[] newBytes, ref byte[] completeCache, int cacheIncreaseStep = DEFAULT_CACHE_INCREASE_STEP)
        {
            var change = DataUpdate(dataSource, out newBytes);

            AddDataToExtendableCache(newBytes, initialDataSize: positionInfo.totalData - newBytes.Length, ref completeCache, cacheIncreaseStep);
            return change;
        }

        #endregion

        #region Handling of adding new data into the data source (updates the Ringbuffer info and so the datasource header)
        public void AddData(IRingBufferDataSource dataSource, byte[] newData)
        {
            var dataSourcePositionsToWrite = AddData(newData.Length);
            int newDataIndex = 0;
            if (dataSourcePositionsToWrite.Length != newData.Length)
            {
                throw new System.Exception("Error in AddData lengths");
            }
            foreach (var dataSourcePositionToWrite in dataSourcePositionsToWrite)
            {
                dataSource.SetDataAt(dataSourcePositionToWrite, newData[newDataIndex]);
                newDataIndex++;
            }
            // Edit data source header bytes
            SaveBufferHeaderToDatasource(dataSource);
        }
        public void AddData(IRingBufferDataSource dataSource, byte[] newData, ref byte[] completeCache, int cacheIncreaseStep = DEFAULT_CACHE_INCREASE_STEP)
        {
            var initialtotalData = positionInfo.totalData;
            AddData(dataSource, newData);

            AddDataToExtendableCache(newData, initialDataSize: initialtotalData, ref completeCache, cacheIncreaseStep);
        }

        // If provided, complete a local global cache of all data
        void AddDataToExtendableCache(byte[] newData, int initialDataSize, ref byte[] completeCache, int cacheIncreaseStep = DEFAULT_CACHE_INCREASE_STEP)
        {
            if (completeCache != null)
            {
                if (completeCache.Length < (initialDataSize + newData.Length))
                {
                    byte[] newCache = new byte[initialDataSize + newData.Length + cacheIncreaseStep];

                    System.Buffer.BlockCopy(completeCache, 0, newCache, 0, completeCache.Length);
                    completeCache = newCache;
                }
                System.Buffer.BlockCopy(newData, 0, completeCache, initialDataSize, newData.Length);
            }
        }
        #endregion

        #endregion

        #region Manipulation of stored bytes as IRingBufferEntry, structured data that can be automatically converted to bytes and added to the ring buffer
        // Interface for structured data that can be automatically converted to bytes and added to the ring buffer
        public interface IRingBufferEntry : IByteArraySerializable {}

        unsafe public Change DataUpdate<T>(IRingBufferDataSource dataSource, out byte[] newPaddingStartBytes, out T[] newEntries, ref byte[] completeCache, int cacheIncreaseStep = DEFAULT_CACHE_INCREASE_STEP) where T : unmanaged, IRingBufferEntry
        {
            var change = DataUpdate(dataSource, out var newBytes, ref completeCache, cacheIncreaseStep);
            // The start of the changes might be the end of data we missed: only the latest size(T) entries will be taken
            Split(newBytes, out newPaddingStartBytes, out newEntries);
            return change;
        }

        unsafe public void Split<T>(byte[] newBytes, out byte[] newPaddingStartBytes, out T[] newEntries) where T : unmanaged, IRingBufferEntry
        {
            Split<T>(newBytes, out newPaddingStartBytes, out newEntries, out _);
        }

        unsafe public void Split<T>(byte[] newBytes, out byte[] newPaddingStartBytes, out T[] newEntries, out int entrySize) where T : unmanaged, IRingBufferEntry
        {
            ByteArrayTools.Split(newBytes, out newPaddingStartBytes, out newEntries, out entrySize);
        }

        unsafe public void AddEntry<T>(IRingBufferDataSource dataSource, T newEntry, ref byte[] completeCache, int cacheIncreaseStep = DEFAULT_CACHE_INCREASE_STEP) where T : unmanaged, IRingBufferEntry
        {
            AddData(dataSource, newEntry.AsByteArray, ref completeCache, cacheIncreaseStep);
        }

        unsafe public void AddEntry<T>(IRingBufferDataSource dataSource, T newEntry) where T : unmanaged, IRingBufferEntry
        {
            AddData(dataSource, newEntry.AsByteArray);
        }

        // Returns how many (full) entry are stored before a given data source position
        public static int EntryIndexAtDataSourcePosition<T>(int pos) where T : unmanaged, IByteArraySerializable
        {
            var entrySize = ByteArrayTools.ByteArrayRepresentationLength<T>();
            var entryCount = DataSourcePositionToIndex(pos) / entrySize;
            return entryCount;
        }

        // Check if the n-th entry, starting from the last one, is still fully included into the ring buffer available data
        private bool IsEntryStillInBuffer(int entrySize, int entryOffset)
        {
            if (positionInfo.firstIndex == -1)
            {
                // No data yet added 
                return false;
            }

            bool bufferFilledOnce = positionInfo.firstIndex == positionInfo.nextIndex;
            int availableBytes = bufferFilledOnce ? indexCount : positionInfo.nextIndex;

            int requiredBytes = entrySize * (entryOffset + 1);

            if (requiredBytes > availableBytes)
            {
                return false;
            }
            return true;
        }

        // Find the last entry (or the entryOffset-th last entry) still present in the ring buffer)
        public bool TryGetLastEntry<T>(IRingBufferDataSource dataSource, int entrySize, out T entry, int entryOffset = 0) where T : unmanaged, IRingBufferEntry
        {
            entry = default;
            if(IsEntryStillInBuffer(entrySize, entryOffset) == false)
            {
                return false;
            }

            int startIndex = (positionInfo.nextIndex - entrySize * (entryOffset + 1)) % indexCount;
            if (startIndex < 0) startIndex += indexCount;

            byte[] entryData = new byte[entrySize];
            for (int i = 0; i < entrySize; i++)
            {
                int targetIndex = (startIndex + i) % indexCount;
                int dataSourcePosition = IndexToDataSourcePosition(targetIndex);
                entryData[i] = dataSource.DataAt(dataSourcePosition);
            }

            entry.FillFromBytes(entryData);
            return true;
        }


        // Replace the last entry (or the entryOffset-th last entry) still present in the ring buffer)
        public bool TryReplaceLastEntry<T>(IRingBufferDataSource dataSource, int entrySize, T replacementEntry, int entryOffset = 0) where T : unmanaged, IRingBufferEntry
        {
            if (IsEntryStillInBuffer(entrySize, entryOffset) == false)
            {
                return false;
            }

            int startIndex = (positionInfo.nextIndex - entrySize * (entryOffset + 1)) % indexCount;
            if (startIndex < 0) startIndex += indexCount;

            byte[] entryData = replacementEntry.AsByteArray;
            for (int i = 0; i < entrySize; i++)
            {
                int targetIndex = (startIndex + i) % indexCount;
                int dataSourcePosition = IndexToDataSourcePosition(targetIndex);
                dataSource.SetDataAt(dataSourcePosition, entryData[i]);
            }

            return true;
        }

        // Calls a callback for all entries still in the buffer, to allow to decide to replace them. If the callback returns null, then the entry is not replaced
        public void EditEntriesStillInbuffer<T>(IRingBufferDataSource dataSource, Func<T, T?> editCallback ) where T : unmanaged, IRingBufferEntry
        {
            int offset = 0;
            var entrySize = ByteArrayTools.ByteArrayRepresentationLength<T>();
            while (TryGetLastEntry(dataSource, entrySize, out T entry, offset))
            {
                var replacement = editCallback(entry);
                if(replacement != null)
                {
                    var newEntry = replacement.GetValueOrDefault();
                    TryReplaceLastEntry(dataSource, entrySize, newEntry, offset);
                }
                offset++;
            }
        }
        #endregion
    }
}
