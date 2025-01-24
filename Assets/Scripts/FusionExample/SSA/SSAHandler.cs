#if OCULUS_SDK_AVAILABLE
using Meta.XR.MRUtilityKit;
#endif

using UnityEngine;


/***
 * 
 * The SSAHandler class provides an OnColocationReady() method that is called when colocation is ready, which can be used to reposition the MRUK scene. 
 * 
 ***/
public class SSAHandler : MonoBehaviour
{
    #if OCULUS_SDK_AVAILABLE
    [SerializeField] bool reenableMRUKWorldLock = false;
    public async void OnColocationReady()
    {
        Debug.Log("Colocation ready");
        // Reposition MRUK scene
        // Reload the scene and enable the EnableWorldLock parameter after the rig teleport
        if (MRUK.Instance != null)
        {
            Debug.Log("Reposition MRUK scene");
            MRUK.Instance.ClearScene();
            if (reenableMRUKWorldLock)
            {
                MRUK.Instance.EnableWorldLock = true;
            }
            await MRUK.Instance.LoadSceneFromDevice();
        }
    }
    #endif
}
