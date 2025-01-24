using System.Collections;
using System.Collections.Generic;
#if POLYSPATIAL_SDK_AVAILABLE
using Unity.PolySpatial;
#endif
using UnityEngine;


namespace Fusion.Addons.VisionOsHelpers
{
    /**
    * 
    * Script to automatically configure a VolumeCamera. 
    * Usefull, in addition to a rig selector, to easily switch between bounded and unbounded volume configuration
    *  
    **/

    public class VolumeCameraConfigurationSelector : MonoBehaviour
    {
#if POLYSPATIAL_SDK_AVAILABLE
    [SerializeField]
    [Tooltip("If true, the active configuration will be checked each update")]
    bool keepConfigurationSelected = true;
    [SerializeField]
    VolumeCamera volumeCamera;
    public VolumeCameraWindowConfiguration volumeConfiguration;
    [SerializeField]
    Transform volumeExpectedPosition;
    [SerializeField]
    Vector3 captureVolume = new Vector3(1f, 1f, 1f);
    [SerializeField]
    List<GameObject> gameObjectsToDisable = new List<GameObject>();
    [SerializeField]
    List<GameObject> gameObjectsToEnable = new List<GameObject>();
    [SerializeField]
    bool disableObjectsToEnableAtStart = true;

    bool firstActivationDone = false;
    private void Awake()
    {
        if(volumeCamera == null)
        {
            volumeCamera = GetComponentInParent<VolumeCamera>();
        }
        if (disableObjectsToEnableAtStart)
        {
            // We disable the objects to enable by default
            foreach (var o in gameObjectsToEnable) if (o) o.SetActive(false);
        }
    }

    public void ActivateConf() {
        if (volumeCamera && (volumeCamera.WindowConfiguration != volumeConfiguration || firstActivationDone == false))
        {
            firstActivationDone = true;
            volumeCamera.WindowConfiguration = volumeConfiguration;
            if (volumeExpectedPosition) {
                volumeCamera.transform.position = volumeExpectedPosition.transform.position;
                volumeCamera.transform.rotation = volumeExpectedPosition.transform.rotation;
            }
            volumeCamera.Dimensions = captureVolume;
            foreach (var o in gameObjectsToDisable) if (o) o.SetActive(false);
            foreach (var o in gameObjectsToEnable) if (o) o.SetActive(true);
        }
    }

    private void Update()
    {
        if (keepConfigurationSelected)
            ActivateConf();
    }
#endif
    }
}
