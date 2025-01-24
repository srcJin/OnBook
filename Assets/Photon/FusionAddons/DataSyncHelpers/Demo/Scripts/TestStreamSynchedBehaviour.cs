using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.DataSyncHelpers;

public class TestStreamSynchedBehaviour : StreamSynchedBehaviour
{

    [Header("Test result")]
    public byte[] allData = new byte[0];
    [Header("Test param")]
    public int testSentRandomDataSize = 5_000;
    public byte[] lastSentData = new byte[0];

    [EditorButton("SendRandomData")]
    public void SendRandomData()
    {
        ByteArrayTools.FillWithRandomInt(ref lastSentData, testSentRandomDataSize);
        Send(lastSentData);
    }

    protected override void OnNewBytes(byte[] newData)
    {
        base.OnNewBytes(newData);
        if (newData.Length == 0) return;

        byte[] newCache = new byte[allData.Length + newData.Length];
        if(allData != null && allData.Length > 0)
        {
            System.Buffer.BlockCopy(allData, 0, newCache, 0, allData.Length);
        }
        System.Buffer.BlockCopy(newData, 0, newCache, allData.Length, newData.Length);
        allData = newCache;
    }
}
