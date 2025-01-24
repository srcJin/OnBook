using System.Collections;
using System.Collections.Generic;
using Fusion.XR.Shared.Rig;
#if POLYSPATIAL_SDK_AVAILABLE
using Unity.PolySpatial;
using UnityEngine.XR.ARFoundation;
#endif
using UnityEngine;

namespace Fusion.Addons.VisionOsHelpers
{
#if POLYSPATIAL_SDK_AVAILABLE
    [RequireComponent(typeof(VolumeCamera))]
#endif
    public class VolumeCameraLocomotion : MonoBehaviour
    {
#if POLYSPATIAL_SDK_AVAILABLE
        HardwareRig hardwareRig;
        List<ARPlaneManager> planeManagers = new List<ARPlaneManager>();

        void FindHardwareRig() {
            if(hardwareRig && hardwareRig.isActiveAndEnabled == false)
            {
                hardwareRig = null;
            }
            if(hardwareRig == null)
            {
                hardwareRig = FindObjectOfType<HardwareRig>();
                planeManagers = new List<ARPlaneManager>(FindObjectsOfType<ARPlaneManager>());
            }
        }

        private void LateUpdate()
        {
            FindHardwareRig();
            if (hardwareRig) {
                // Move the camera
                transform.position = hardwareRig.transform.position;
                transform.rotation = hardwareRig.transform.rotation;
                // Move the plane managers
                foreach(var planeManager in planeManagers)
                {
                    planeManager.transform.position = hardwareRig.transform.position;
                    planeManager.transform.rotation = hardwareRig.transform.rotation;
                }
            }
        }
#endif
    }
}
