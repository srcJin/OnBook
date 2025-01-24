using Fusion.XR.Shared.Editor;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.XRHandsSync.Editor
{
    public static class XRHandsSynchronizationAutomation
    {
        const string leftHardwareHandGuid = "ff123badd9cf3374aabb84c2a0f5e863";
        const string rightHarwareHandGuid = "fa6d443e3dbb7554b99c8a5689e9ee74";
        const string leftNetworkHandGuid = "07bc4ff183c2ca045ba367f0db09f12b";
        const string rightNetworkHandGuid = "dafd527bd4f13f649b9cba1b115719d5";
        const string fingerTrackingActionsGuid = "a9c28d8ba6718b846b9fcb0467d0f2eb";

        public static bool InstalFingerTrackingActions = true;

        public static bool IsHandsModelsOnHardwareRig()
        {
            if (XRSharedAutomation.TryGetHardwareRig(out var hardwareRig) == false)
            {
                return false;
            }
            var isOnLeftHands = XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.leftHand.gameObject, leftHardwareHandGuid, out _);
            var isOnRightHands = XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.rightHand.gameObject, rightHarwareHandGuid, out _);
            return isOnLeftHands && isOnRightHands;
        }

        public static void AddFingerModelsToHardwareHands(out GameObject leftHandModels, out GameObject rightHandModels, out GameObject leftFingerTrackingActions, out GameObject rightFingerTrackingActions)
        {
            leftHandModels = null; rightHandModels = null;
            leftFingerTrackingActions = null; rightFingerTrackingActions = null;
            //XRHelpersWindow.ShowXRHelpers();
            if (XRSharedAutomation.TryGetHardwareRig(out var hardwareRig) == false)
            {
                return;
            }

            foreach (var hand in new HardwareHand[] { hardwareRig.leftHand, hardwareRig.rightHand })
            {
                foreach (Transform child in hand.transform)
                {
                    if (child.TryGetComponent<Collider>(out var collider) && child.GetComponents<Component>().Length == 2 && child.childCount == 0)
                    {
                        // Collider only child: we remove it
                        Debug.Log("Removing hand collide: will re replaced by context (finger/controller tracking) sensitive ones");
                        try
                        {
                            GameObject.DestroyImmediate(child.gameObject);
                        }
#pragma warning disable CS0168
                        catch (Exception _)
#pragma warning restore CS0168
                        {
                        // Unable to destroy, disable
                            child.gameObject.SetActive(false);
                        }
                        break;
                    }
                }
            }
            //TODO: remove hardware osf hands if any

            if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.leftHand.gameObject, leftHardwareHandGuid, out leftHandModels))
            {
                Debug.LogError("Hand models already in place on left hand");
            }
            else
            {
                XRProjectAutomation.SpawnChildIfnotPresent(hardwareRig.leftHand, leftHardwareHandGuid, out leftHandModels);
            }

            if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.rightHand.gameObject, rightHarwareHandGuid, out rightHandModels))
            {
                Debug.LogError("Hand models already in place on right hand");
            }
            else
            {
                XRProjectAutomation.SpawnChildIfnotPresent(hardwareRig.rightHand, rightHarwareHandGuid, out rightHandModels);
            }

            if (InstalFingerTrackingActions)
            {
                if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.leftHand.gameObject, fingerTrackingActionsGuid, out leftFingerTrackingActions))
                {
                    Debug.LogError("Finger tracking actions already in place on left hand");
                }
                else
                {
                    XRProjectAutomation.SpawnChildIfnotPresent(hardwareRig.leftHand, fingerTrackingActionsGuid, out leftFingerTrackingActions);
                }
            }
            if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hardwareRig.rightHand.gameObject, fingerTrackingActionsGuid, out rightFingerTrackingActions))
            {
                Debug.LogError("Finger tracking actions already in place on right hand");
            }
            else
            {
                XRProjectAutomation.SpawnChildIfnotPresent(hardwareRig.rightHand, fingerTrackingActionsGuid, out rightFingerTrackingActions);
            }
        }

        public static void AddFingerModelsToNetworkHands()
        {
            string assetPath = XRSharedAutomation.CurrentUserSpawnerPrefabPath();
            if (assetPath == null)
            {
                Debug.LogError("Add a connection manager and a network rig set in it to the scene first");
                return;
            }

            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
            NetworkRig networkRig = contentsRoot.GetComponent<NetworkRig>();



            // Remove osf hands if any
            foreach (var hand in new NetworkHand[] { networkRig.leftHand, networkRig.rightHand })
            {
                if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hand.gameObject, XRSharedAutomation.leftOSFHandGuid, out var osfHandInstance))
                {
                    Debug.Log("Removing OSF hand");
                    GameObject.DestroyImmediate(osfHandInstance.gameObject);
                }
                if (XRProjectAutomation.TryFindPrefabInstanceInChildren(hand.gameObject, XRSharedAutomation.rightOSFHandGuid, out osfHandInstance))
                {
                    Debug.Log("Removing OSF hand");
                    GameObject.DestroyImmediate(osfHandInstance.gameObject);
                }
            }

            if (XRProjectAutomation.TryFindPrefabInstanceInChildren(networkRig.leftHand.gameObject, leftNetworkHandGuid, out var prefabInstance))
            {
                Debug.LogError("Hand models already in place on left hand");
            }
            else
            {
                XRProjectAutomation.SpawnChildIfnotPresent(networkRig.leftHand, leftNetworkHandGuid);
            }

            if (XRProjectAutomation.TryFindPrefabInstanceInChildren(networkRig.leftHand.gameObject, rightNetworkHandGuid, out prefabInstance))
            {
                Debug.LogError("Hand models already in place on right hand");
            }
            else
            {
                XRProjectAutomation.SpawnChildIfnotPresent(networkRig.rightHand, rightNetworkHandGuid);
            }

            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }

}
