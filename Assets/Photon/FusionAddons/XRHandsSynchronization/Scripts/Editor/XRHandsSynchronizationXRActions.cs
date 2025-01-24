#if FUSION_WEAVER
using Fusion.Addons.HandsSync;
using Fusion.Addons.XRHandsSync.Editor;
using Fusion.XR.Shared.Editor;
using Fusion.XR.Shared.Rig;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


[InitializeOnLoad]
class XRHandsSynchronizationXRActions
{
    struct AddHandsModelToHardwareRigXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "Hands models <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> hands models";
        public string CategoryName => XRActionsManager.HARDWARERIG_CATEGORY;
        public string ImageName => "xrguide-add-hardwarerig-handmodels";
        public int Weight => 20;
        public bool IsInstalled
        {
            get
            {
                return XRHandsSynchronizationAutomation.IsHandsModelsOnHardwareRig();
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
                if (XRSharedAutomation.TryGetHardwareRig(out var hardwareRig))
                {
                    var leftHand = hardwareRig.leftHand.GetComponentInChildren<HardwareHandRepresentationManager>();
                    var rightHand = hardwareRig.rightHand.GetComponentInChildren<HardwareHandRepresentationManager>();
                    if (Selection.activeObject == leftHand)
                    {
                        Selection.activeObject = rightHand;
                    }
                    else
                    {
                        Selection.activeObject = leftHand;
                    }

                }
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRProjectAutomation.ExitPrefabMode();
                XRHandsSynchronizationAutomation.AddFingerModelsToHardwareHands(out GameObject leftHandModels, out GameObject rightHandModels, out GameObject leftFingerTrackingActions, out GameObject rightFingerTrackingActions);
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[finger tracking detection]</color> to the hardware rig's hands\n- detects finger tracking usage and collect finger bones rotations", selecter: this, imageName: ImageName);
                if (XRHandsSynchronizationAutomation.InstalFingerTrackingActions)
                {
                    XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[gesture detection]</color> to the hardware rig's hands\n- detects finger gestures, such as pinch. Triggers grab on pinch", associatedObject: leftFingerTrackingActions, imageName: ImageName, forceExitPrefabMode: true);
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

    }

    struct AddHandsModelToNetworkRigXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "Hands models <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> hands models";
        public string CategoryName => XRActionsManager.NETWORKRIG_CATEGORY;
        public string ImageName => "xrguide-add-networkrig-handmodels";
        public int Weight => 20;
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
                if (userSpawner.UserPrefab.GetComponentInChildren<NetworkHandRepresentationManager>())
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
                Selection.activeObject = stage.FindComponentOfType<NetworkHandRepresentationManager>()?.gameObject;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRHandsSynchronizationAutomation.AddFingerModelsToNetworkHands();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[hands models]</color> to the network rig's hands\n- displays either a finger tracking based hand model, or a controller animated one", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }

    static AddHandsModelToHardwareRigXRAction addHandsModelToHardwareRigXRAction;
    static AddHandsModelToNetworkRigXRAction addHandsModelToNetworkRigXRAction;

    static XRHandsSynchronizationXRActions()
    {
        XRActionsManager.RegisterAction(addHandsModelToHardwareRigXRAction);
        XRActionsManager.RegisterAction(addHandsModelToNetworkRigXRAction);
    }
}
#endif