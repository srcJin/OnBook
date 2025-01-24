using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using Fusion.Addons.DataSyncHelpers;

public class TestRingBufferSync : RingBufferSyncBehaviour<TestRingBufferSync.SampleEntry>
{
    public List<SampleEntry> lastEntries = new List<SampleEntry>();
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
        PushEntryInLocalCache(entry);
    }

    void PushEntryInLocalCache(SampleEntry e)
    {
        lastEntries.Add(e);
        while (lastEntries.Count > maxRingBufferEntries)
        {
            lastEntries.RemoveAt(0);// shift left
        }
    }

    public override void OnNewEntries(byte[] newPaddingStartBytes, TestRingBufferSync.SampleEntry[] newEntries)
    {
        int i = 0;
        foreach (var entry in newEntries)
        {
            Log($"[{i}]New entry: {entry.pos}", LogLevel.Test);
            i++;
            PushEntryInLocalCache(entry);
        }
    }

    public override void OnDataloss(RingBuffer.LossRange lossRange) {
        Debug.LogError($"Data loss detected: no loss recovery strategy here, won't be recovered. [{lossRange.start} - {lossRange.end} bytes]");
    }
    #endregion
}
