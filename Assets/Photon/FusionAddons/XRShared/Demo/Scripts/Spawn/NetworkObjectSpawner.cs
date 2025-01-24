using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField] NetworkObject prefab;
    [SerializeField] float spawnDistance = 0.2f;
    [Networked]
    public NetworkObject CurrentInstance { get; set; }

    private void Update()
    {
        if(Object != null && CurrentInstance != null && Object.HasStateAuthority && Vector3.Distance(transform.position, CurrentInstance.transform.position) > spawnDistance)
        {
            Debug.LogError("Spawning due to distance");
            CurrentInstance = Runner.Spawn(prefab, transform.position, transform.rotation);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority && CurrentInstance == null)
        {
            CurrentInstance = Runner.Spawn(prefab, transform.position, transform.rotation);

        }
    }
}


