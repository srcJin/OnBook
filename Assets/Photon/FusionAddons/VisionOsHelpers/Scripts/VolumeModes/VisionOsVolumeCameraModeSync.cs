using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.Events;
using Fusion.Addons.HandsSync;
#if UNITY_VISIONOS
using Unity.PolySpatial;
#endif

namespace Fusion.Addons.VisionOsHelpers
{
    /**
    * 
    * Script to hide local avatar parts based on mode (renderers should be hidden in bounded volume as the user position is undefined).
    * Also, it provides the possibility to disable the hand tracking in bounded volume (hand model would then disappear while in bounded mode, due to NetworkHandRepresentationManager logic).
    *  
    **/
    public class VisionOsVolumeCameraModeSync : NetworkBehaviour
    {
        public enum VisionOsMode
        {
            Undefined,
            VisionOSBoundedVolume,
            VisionOSUnboundedVolume
        }

        [Networked]
        public VisionOsMode CurrentMode { get; set; }

        VisionOsMode appliedMode = VisionOsMode.Undefined;

        [SerializeField]
        bool hideAllChildRenderersInBoundedVolume = true;
        [SerializeField]
        bool disableHandTrackingInBoundedMode = true;

        [SerializeField]
        List<Renderer> hiddenRenderersForBoundedVolume = new List<Renderer>();
        [SerializeField]
        List<NetworkHandRepresentationManager> handRepresentationManagers = new List<NetworkHandRepresentationManager>();

        public UnityEvent onModeChange;

#if UNITY_VISIONOS
    VolumeCamera localVolumeCamera;
#endif

        public override void Spawned()
        {
            base.Spawned();
            if (hideAllChildRenderersInBoundedVolume)
            {
                hiddenRenderersForBoundedVolume = new List<Renderer>();
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.enabled) hiddenRenderersForBoundedVolume.Add(r);
                }
            }
            if (disableHandTrackingInBoundedMode)
            {
                handRepresentationManagers = new List<NetworkHandRepresentationManager>(GetComponentsInChildren<NetworkHandRepresentationManager>(true));
            }

            if (Object.HasStateAuthority)
            {
                DetectVolumeCamera();
            }
            AdaptRenderers();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (Object.HasStateAuthority)
            {
                DetectVolumeCamera();
            }
        }

        public override void Render()
        {
            base.Render();
            CheckMode();
            if (disableHandTrackingInBoundedMode)
            {
                foreach (var handrepresentationManager in handRepresentationManagers)
                {
                    handrepresentationManager.forceDisableHandTracking = CurrentMode == VisionOsMode.VisionOSBoundedVolume;
                }
            }
        }

        void DetectVolumeCamera()
        {
            if (Object.HasStateAuthority)
            {
                var mode = VisionOsMode.Undefined;
#if UNITY_VISIONOS
            if (localVolumeCamera == null) localVolumeCamera = FindObjectOfType<VolumeCamera>();
            if (localVolumeCamera) {
                if (localVolumeCamera.WindowConfiguration.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Bounded)
                {
                    mode = VisionOsMode.VisionOSBoundedVolume;
                }
                if (localVolumeCamera.WindowConfiguration.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)
                {
                    mode = VisionOsMode.VisionOSUnboundedVolume;
                }
            }
#endif
                if (mode != CurrentMode)
                {
                    Debug.Log("Detected local VolumeCamera Mode change " + mode);
                    CurrentMode = mode;
                }
            }
        }

        void CheckMode()
        {
            if (appliedMode != CurrentMode)
            {
                AdaptRenderers();
                if (onModeChange != null) onModeChange.Invoke();
            }
        }

        void AdaptRenderers()
        {
            foreach (var r in hiddenRenderersForBoundedVolume)
            {
                var visible = CurrentMode != VisionOsMode.VisionOSBoundedVolume;
                r.enabled = visible;
            }

            appliedMode = CurrentMode;
        }

    }
}
