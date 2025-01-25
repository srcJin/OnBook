using NaughtyAttributes;
using Scripts.Utilities;
using UnityEngine;
using Fusion;

public class NetworkTestingManager : Singleton<NetworkTestingManager>
{
    private NetworkRunner runner;
    
    public GameObject testNetworkedObject;
    
    [Button]
    public void CreateTestNetworkedObject()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
        }
        
        Vector3 forwardPosition = GameCameraCache.MainGameObject.transform.position + GameCameraCache.MainGameObject.transform.forward * 1.5f;
        
        var createdNetworkObj = runner.Spawn(testNetworkedObject,
            Vector3.zero,
            Quaternion.identity,
            runner.LocalPlayer);
    }
}
