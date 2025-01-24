using UnityEngine;
using Fusion;
using Fusion.XR.Shared;

/***
 * 
 * The SpawnOnConnect class manages the automatic spawning of a networked object when a player connects to the session. 
 * Upon being spawned, it checks for state authority and spawns a specified prefab at the current position and rotation after an optional delay. 
 * After spawning the new object, the original object is despawned.
 * 
 ***/
public class SpawnOnConnect : NetworkBehaviour
{
    [SerializeField] NetworkObject prefab;

    [SerializeField] int delayMS = 1000;

    public async override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority)
        {
            if (delayMS > 0) await AsyncTask.Delay(delayMS);
            if (prefab) Runner.Spawn(prefab, transform.position, transform.rotation);
            Runner.Despawn(Object);
        }
    }
}
