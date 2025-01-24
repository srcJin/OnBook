#if POLYSPATIAL_SDK_AVAILABLE
using Fusion.Addons.VisionOsHelpers;
using Fusion.Addons.VisionOsHelpers.Editor;
using Fusion.XR.Shared.Editor;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using Unity.PolySpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class VisionOSXRActions
{
    public static void SetUpVisionOs<T>(T action) where T: IXRActionSelecter, IXRAction
    {
        if (VisionOSAutomation.TryGetARSession(out _) == false)
        {
            var arSession = VisionOSAutomation.AddARSession();
            XRActionsManager.AddLog("Add an ARFundation's <color=HIGHLIGHT_COLOR>[ARSession]</color>\n- required by Polyspatial for rig parts tracking", associatedObject: arSession, imageName: action.ImageName);
        }
        VisionOSAutomation.ConfigureCameraForVisionOS(out Camera cam, out bool arCameraManagerAdded, out bool arCameraBackgroundAdded, out bool cameraConfigurationChanged);
        if (cam)
        {
            if (arCameraManagerAdded)  XRActionsManager.AddLog("Add an ARFundation's <color=HIGHLIGHT_COLOR>[ARCameraManager]</color>\n- required by Polyspatial for camera", associatedObject: cam, imageName: action.ImageName);
            if(arCameraBackgroundAdded) XRActionsManager.AddLog("Add an ARFundation's <color=HIGHLIGHT_COLOR>[ARCameraBackground]</color>\n- required by Polyspatial for camera", associatedObject: cam, imageName: action.ImageName);
            if(cameraConfigurationChanged) XRActionsManager.AddLog("Configure <color=HIGHLIGHT_COLOR>[camera rendering depth option and background]</color>\n- required by Polyspatial for camera", associatedObject: cam, imageName: action.ImageName);
        } else
        {
            Debug.LogError("Missing camera");
        }
    }

    struct CreateSpatialGrabberXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "<color=#A7A7A7>Select</color> spatial grabber" : "<color=#A7A7A7>Add</color> spatial grabber";

        public string CategoryName => XRActionsManager.HARDWARERIG_CATEGORY;
        public int Weight => 200;

        public string ImageName => "xrguide-add-spatialgrabber";

        public bool IsInstalled
        {
            get
            {
                return VisionOSAutomation.TryGetSpatialGrabber(out _);
            }
        }

        public bool IsActionVisible
        {
            get
            {
                return true;
            }
        }

        public bool TrySelect()
        {
            if (IsInstalled)
            {
                XRProjectAutomation.ExitPrefabMode();
                VisionOSAutomation.TryGetSpatialGrabber(out var spatialGrabber);
                Selection.activeObject = spatialGrabber;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRProjectAutomation.ExitPrefabMode();
                var o = VisionOSAutomation.CreateSpatialGrabber();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[spatial touch handler]</color>\n- allows to detect and react to visionOS indirect pinches", selecter: this, imageName: ImageName);
                SpatialGrabber grabber = o == null ? null : o.GetComponentInChildren<SpatialGrabber>();
                if (grabber)
                {
                    XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[spatial grabber]</color>\n- adds a virtual hand, to grab object from afar with visionOS indirect pinches", associatedObject: grabber, imageName: ImageName, forceExitPrefabMode: true);
                }
                VisionOSAutomation.AddVisionOSHandsConfigurationToHardwareHands(out VisionOSHandsConfiguration addedLeftConf, out VisionOSHandsConfiguration addedRightConf);
                if (addedLeftConf)
                {
                    XRActionsManager.AddLog("Change <color=HIGHLIGHT_COLOR>[hands collider layers] (VisionOSHandsConfiguration)</color>\n- <color=ALERT_COLOR>Note: you need to add a PolySpatialIgnored layer. Will be placed to hand collider. Should not be in polyspatial collider layer mask</color>", associatedObject: addedLeftConf, imageName: ImageName, forceExitPrefabMode: true);
                    XRActionsManager.AddLog("Change <color=HIGHLIGHT_COLOR>[XR hand grabbing configuration] (VisionOSHandsConfiguration)</color>\n- ensure that even if the hands are not detected for a short duration, the grabbing still continues", associatedObject: addedLeftConf, imageName: ImageName, forceExitPrefabMode: true);
                    XRActionsManager.AddLog("Adapt <color=HIGHLIGHT_COLOR>[ray beamer lineRenderer] (VisionOSHandsConfiguration)</color>\n- use a replacement for LineRenderer, not supported on visionOS", associatedObject: addedLeftConf, imageName: ImageName, forceExitPrefabMode: true);
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }

    struct CreateUnboundedVolumeXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "<color=#A7A7A7>Select</color> unbounded volume camera" : "<color=#A7A7A7>Add</color> volume camera <color=#A7A7A7>(unbounded)</color>";

        public string CategoryName => XRActionsManager.SCENE_OBJECT_CATEGORY;
        public int Weight => 200;

        public string ImageName => "xrguide-add-unboundedvolume";

        public bool IsInstalled
        {
            get
            {
                return VisionOSAutomation.TryGetVolumeCamera(out _, mode: VolumeCamera.PolySpatialVolumeCameraMode.Unbounded);
            }
        }

        public bool IsActionVisible => true;
        public bool TrySelect()
        {
            if (IsInstalled)
            {
                XRProjectAutomation.ExitPrefabMode();
                VisionOSAutomation.TryGetVolumeCamera(out var volumeCamera, mode: VolumeCamera.PolySpatialVolumeCameraMode.Unbounded);
                Selection.activeObject = volumeCamera;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRProjectAutomation.ExitPrefabMode();
                SetUpVisionOs(this);
                var o = VisionOSAutomation.CreateUnboundedVolumeCamera();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[unbounded volume camera]</color>\n- required by Polyspatial. <color=ALERT_COLOR>Note: you need to select Mixed reality in XR plug-inManagement > Apple visionOS > App mode</color>", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
    struct CreateBoundedVolumeXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "<color=#A7A7A7>Select</color> bounded volume camera" : "<color=#A7A7A7>Add</color> volume camera <color=#A7A7A7>(bounded)</color>";

        public string CategoryName => XRActionsManager.SCENE_OBJECT_CATEGORY;
        public int Weight => 210;

        public string ImageName => "xrguide-add-boundedvolume";

        public bool IsInstalled
        {
            get
            {
                return VisionOSAutomation.TryGetVolumeCamera(out _, mode: VolumeCamera.PolySpatialVolumeCameraMode.Bounded);
            }
        }

        public bool IsActionVisible => true;
        public bool TrySelect()
        {
            if (IsInstalled)
            {
                XRProjectAutomation.ExitPrefabMode();
                VisionOSAutomation.TryGetVolumeCamera(out var volumeCamera, mode: VolumeCamera.PolySpatialVolumeCameraMode.Bounded);
                Selection.activeObject = volumeCamera;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRProjectAutomation.ExitPrefabMode();
                SetUpVisionOs(this);
                var o = VisionOSAutomation.CreateBoundedVolumeCamera();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[bounded volume camera]</color>\n- required by Polyspatial. <color=ALERT_COLOR>Note: you need to select Mixed reality in XR plug-inManagement > Apple visionOS > App mode</color>", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }

    struct AddSpatialGrabberToNetworkRigXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "Spatial Grabber <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> spatial grabber";
        public string CategoryName => XRActionsManager.NETWORKRIG_CATEGORY;
        public string ImageName => "xrguide-add-spatial-grabber-network";
        public int Weight => 200;
        public bool IsInstalled
        {
            get
            {
                var userSpawner = XRSharedAutomation.CurrentUserSpawner();
                if (userSpawner == null || userSpawner.UserPrefab == null)
                {
                    return false;
                }


                bool found = false;
                if (XRProjectAutomation.TryFindPrefabInstanceInChildren(userSpawner.UserPrefab.gameObject, VisionOSAutomation.spatialGrabberNetworkRigGuid, out _))
                {
                    found = true;
                }

                return found;

            }
        }
        public bool IsActionVisible
        {
            get
            {
                return true;
            }
        }

        public bool TrySelect()
        {
            if (IsInstalled)
            {
                var userSpawner = XRSharedAutomation.CurrentUserSpawner();
                string assetPath = AssetDatabase.GetAssetPath(userSpawner.UserPrefab);
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
                var stage = PrefabStageUtility.GetCurrentPrefabStage();

                var networkRig = stage.FindComponentOfType<NetworkRig>();
                if (networkRig && XRProjectAutomation.TryFindPrefabInstanceInChildren(networkRig.gameObject, VisionOSAutomation.spatialGrabberNetworkRigGuid, out var spatialGrabber))
                {
                    Selection.activeObject = spatialGrabber;
                }
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                VisionOSAutomation.AddSpatialGrabberToNetworkRig();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[spatial grabber]</color> to NetworkRig\n- synchronize the virtual hand position, to move objects from afar with visionOS indirect pinches", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }

    static CreateSpatialGrabberXRAction createSpatialGrabberXRAction;
    static AddSpatialGrabberToNetworkRigXRAction addSpatialGrabberToNetworkRigXRAction;
    static CreateUnboundedVolumeXRAction createUnboundedVolumeXRAction;
    static CreateBoundedVolumeXRAction createBoundedVolumeXRAction;

    static VisionOSXRActions()
    {

        XRActionsManager.RegisterAction(createSpatialGrabberXRAction);
        XRActionsManager.RegisterAction(addSpatialGrabberToNetworkRigXRAction);
        XRActionsManager.RegisterAction(createUnboundedVolumeXRAction);
        XRActionsManager.RegisterAction(createBoundedVolumeXRAction);
    }
}
#endif
