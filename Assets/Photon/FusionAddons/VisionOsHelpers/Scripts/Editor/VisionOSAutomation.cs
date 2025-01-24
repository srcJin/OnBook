#if POLYSPATIAL_SDK_AVAILABLE
using Fusion.XR.Shared.Editor;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using Unity.PolySpatial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

namespace Fusion.Addons.VisionOsHelpers.Editor
{
    public static class VisionOSAutomation
    {
        public const string spatialGrabberGuid = "52fe05ddae9de4c67927c89c725a978a";
        public const string spatialGrabberNetworkRigGuid = "27bf87a4ef7433b4caa7a7597ffdc502";
        public const string unboundedVolumeCameraGuid = "71de466aa6c434d56bc8e6431a002cbd";
        public const string boundedVolumeCameraGuid = "548e901d0bd9b4756acf72bb299011ef";
        public const string arSessionGuid = "7ed193f5fa4814fe3a9482149c2fd845";

        public static void AddSpatialGrabberToNetworkRig()
        {
            string assetPath = XRSharedAutomation.CurrentUserSpawnerPrefabPath();
            if (assetPath == null)
            {
                Debug.LogError("Add a connection manager and a network rig set in it to the scene first");
                return;
            }

            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
            NetworkRig networkRig = contentsRoot.GetComponent<NetworkRig>();

            XRProjectAutomation.SpawnChildIfnotPresent(networkRig, spatialGrabberNetworkRigGuid);

            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }

        public static GameObject CreateSpatialGrabber()
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(spatialGrabberGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject spatialGrabber = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spatialGrabber.transform.SetAsLastSibling();
            return spatialGrabber;
        }

        public static void AddVisionOSHandsConfigurationToHardwareHands(out VisionOSHandsConfiguration addedLeftConf, out VisionOSHandsConfiguration addedRightConf)
        {
            addedLeftConf = null;
            addedRightConf = null;
            if (XRSharedAutomation.TryGetHardwareRig(out var hardwareRig))
            {
                if (hardwareRig.leftHand.GetComponent<VisionOSHandsConfiguration>() == null)
                {
                    addedLeftConf = hardwareRig.leftHand.gameObject.AddComponent<VisionOSHandsConfiguration>();
                }
                if (hardwareRig.rightHand.GetComponent<VisionOSHandsConfiguration>() == null)
                {
                    addedLeftConf = hardwareRig.rightHand.gameObject.AddComponent<VisionOSHandsConfiguration>();
                }
            }
        }

        public static bool TryGetARSession(out ARSession arSession)
        {
            arSession = GameObject.FindObjectOfType<ARSession>();
           return arSession != null;
        }

        public static void ConfigureCameraForVisionOS(out Camera cam, out bool arCameraManagerAdded, out bool arCameraBackgroundAdded, out bool cameraConfigurationChanged) {
            arCameraManagerAdded = false;
            arCameraBackgroundAdded = false;
            cameraConfigurationChanged = false;
            XRSharedAutomation.TryGetHardwareRig(out var hardwareRig);
            cam = hardwareRig == null ? null : hardwareRig.headset.GetComponentInChildren<Camera>();
            if (cam)
            {
                if (cam.GetComponent<ARCameraManager>() == null)
                {
                    arCameraManagerAdded = true;
                    cam.gameObject.AddComponent<ARCameraManager>();
                }
                if (cam.GetComponent<ARCameraBackground>() == null)
                {
                    arCameraBackgroundAdded = true;
                    cam.gameObject.AddComponent<ARCameraBackground>();
                }
                var urpCamData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urpCamData == null)
                {
                    urpCamData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                if (urpCamData)
                {
                    var backgroundColor = new Color(0, 0, 0, 0);
                    if (urpCamData.requiresDepthOption != CameraOverrideOption.On) cameraConfigurationChanged = true;
                    if (cam.clearFlags != CameraClearFlags.SolidColor) cameraConfigurationChanged = true;
                    if (cam.backgroundColor != backgroundColor) cameraConfigurationChanged = true;
                    urpCamData.requiresDepthOption = CameraOverrideOption.On;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = backgroundColor;
                }
                else
                {
                    Debug.LogError("Missing UniversalAdditionalCameraData");
                }
            } 
            else
            {
                Debug.LogError("Missing camera");
            }
        }

        public static ARSession AddARSession()
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(arSessionGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject arSessionGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (arSessionGO == null) return null;
            arSessionGO.transform.SetAsLastSibling();
            return arSessionGO.GetComponentInChildren<ARSession>();
        }

        public static void RemoveVolumeCamera()
        {
            // The scene can contain only one volume camera
            var volumeCameras = GameObject.FindObjectsOfType<VolumeCamera>();
            for(int i = 0; i < volumeCameras.Length; i++){
                GameObject.DestroyImmediate(volumeCameras[i].gameObject);
            }
        }

        public static GameObject CreateUnboundedVolumeCamera()
        {
            RemoveVolumeCamera();
            var prefabPath = AssetDatabase.GUIDToAssetPath(unboundedVolumeCameraGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject volumeCameraGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            volumeCameraGO.transform.SetAsLastSibling();
            SynchronizeVolumeCameraCullingMask(volumeCameraGO.GetComponentInChildren<VolumeCamera>());
            return volumeCameraGO;
        }

        public static GameObject CreateBoundedVolumeCamera()
        {
            RemoveVolumeCamera();
            var prefabPath = AssetDatabase.GUIDToAssetPath(boundedVolumeCameraGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject volumeCameraGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            volumeCameraGO.transform.SetAsLastSibling();
            SynchronizeVolumeCameraCullingMask(volumeCameraGO.GetComponentInChildren<VolumeCamera>());
            return volumeCameraGO;
        }

        public static void SynchronizeVolumeCameraCullingMask(VolumeCamera volumeCamera)
        {
            XRSharedAutomation.TryGetHardwareRig(out var hardwareRig);
            Camera cam = hardwareRig == null ? null : hardwareRig.GetComponentInChildren<Camera>();
            if (volumeCamera && cam)
            {
                volumeCamera.CullingMask = cam.cullingMask;
            }
        }

        public static bool TryGetSpatialGrabber(out SpatialTouchHandler spatialTouchHandler)
        {
            spatialTouchHandler = null;
            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent<SpatialTouchHandler>(out spatialTouchHandler))
            {

            }
            else
            {
                var spatialTouchHandlers = GameObject.FindObjectsOfType<SpatialTouchHandler>();
                if (spatialTouchHandlers.Length == 0)
                {
                    return false;
                }
                else if (spatialTouchHandlers.Length != 1)
                {
                    Debug.LogError("Several SpatialTouchHandler: select one");
                    return false;
                }
                else
                {
                    spatialTouchHandler = spatialTouchHandlers[0];
                }
            }
            return true;
        }

        public static bool TryGetVolumeCamera(out VolumeCamera volumeCamera, VolumeCamera.PolySpatialVolumeCameraMode mode)
        {
            volumeCamera = null;
            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent<VolumeCamera>(out var selectedVolume) && selectedVolume.WindowConfiguration && selectedVolume.WindowConfiguration.Mode == mode)
            {
                volumeCamera = selectedVolume;
                return true;
            }
            else
            {
                var volumeCameras = GameObject.FindObjectsOfType<VolumeCamera>();
                foreach(var v in volumeCameras){
                    if (v.WindowConfiguration && v.WindowConfiguration.Mode == mode)
                    {
                        volumeCamera = v;
                        return true;
                    }
                }
            }
            return false;
        }
    }

}
#endif