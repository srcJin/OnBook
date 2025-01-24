using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using Fusion.XR.Shared;
using Fusion.Addons.DataSyncHelpers;

public class TestLosslessRingBufferSync : RingBufferLosslessSyncBehaviour<TestLosslessRingBufferSync.SampleEntry>
{
    public List<SampleEntry> entries = new List<SampleEntry>();
    [Header("Test")]
    public int testDataLength = 200;
    public int testEntriesLength = 83;
    public SampleEntry singleEntryToAdd;

    #region Entry
    [System.Serializable]
    public struct SampleEntry : RingBuffer.IRingBufferEntry
    {
        public Vector3 pos;

        public byte[] AsByteArray => SerializationTools.AsByteArray(pos);

        public void FillFromBytes(byte[] entryBytes)
        {
            int unserializePosition = 0;
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out pos);
        }

    }

    #endregion

    #region Test

    [EditorButton("AddSingleEntry")]
    public void AddSingleEntry()
    {
        if (Object.HasStateAuthority == false)
        {
            throw new System.Exception("Unable to add entry: not state auth");
        }
        Log("Adding pos: " + singleEntryToAdd.pos, LogLevel.Test);
        AddEntry(singleEntryToAdd);
    }

    [EditorButton("AddRandomEntry")]
    public void AddRandomEntry()
    {
        if (Object.HasStateAuthority == false)
        {
            throw new System.Exception("Unable to add entry: not state auth");
        }
        Vector3 pos = UnityEngine.Random.insideUnitSphere;
        Log("Adding pos: " + pos, LogLevel.Test);
        AddEntry(new SampleEntry { pos = pos });
    }

    [EditorButton("AddSeveralRandomEntries")]
    public void AddSeveralRandomEntries()
    {
        if (Object.HasStateAuthority == false)
        {
            throw new System.Exception("Unable to add entry: not state auth");
        }
        for (int i = 0; i < testEntriesLength; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere;
            Log($"[{i}] Adding pos: {pos}", LogLevel.Test);
            AddEntry(new SampleEntry { pos = pos });
        }
    }
    #endregion

    #region Data handling
    public override void AddEntry(SampleEntry entry)
    {
        base.AddEntry(entry);
        entries.Add(entry);
    }

    public override void OnNewEntries(byte[] newPaddingStartBytes, TestLosslessRingBufferSync.SampleEntry[] newEntries)
    {
        int i = 0;
        foreach (var entry in newEntries)
        {
            Log($"[{i}]New entry: {entry.pos}", LogLevel.Test);
            i++;
            if (lossRanges.Count == 0)
            {
                // No waiting loss request: we can add the entry
                entries.Add(entry);
            }
        }
    }

    protected override void OnNoLossRemaining()
    {
        var entriesArray = SplitCompleteData();
        Debug.LogError($"Received all data: splitting it to {entriesArray.Length} entries (new entries: {entriesArray.Length - entries.Count})");
        entries = new List<SampleEntry>(entriesArray);
    }


    public override void OnNoAnswerForALossRequest(LossRequest request)
    {
        base.OnNoAnswerForALossRequest(request);
        Debug.LogError($"Data lost and unrecoverable (no one has it anymore [{request.lossRange.start} {request.lossRange.end}])");
    }
    #endregion
}
