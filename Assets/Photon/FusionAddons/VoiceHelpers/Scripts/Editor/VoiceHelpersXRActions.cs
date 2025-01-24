#if FUSION_WEAVER
using Fusion.Addons.VoiceHelpers;
using Fusion.Addons.VoiceHelpers.Editor;
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
class VoiceHelpersXRActions
{
    struct AddVoiceToRunnerXRAction : IXRAction, IXRActionSelecter
    {
        public string Description => IsInstalled ? "Voice <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> voice";
        public string CategoryName => XRActionsManager.RUNNER_CATEGORY;
        public string ImageName => "xrguide-add-runnervoice";
        public bool IsInstalled
        {
            get
            {
                return VoiceHelpersAutomation.IsVoiceOnRunner();
            }
        }
        public int Weight => 10;
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
                Selection.activeObject = XRSharedAutomation.CurrentUserSpawner() as UnityEngine.Object;
                return true;
            }
            return false;
        }
        public void Trigger()
        {
            if (TrySelect() == false)
            {
                XRProjectAutomation.ExitPrefabMode();
                VoiceHelpersAutomation.AddVoiceToRunner(out var recorder);
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[FusionVoiceClient]</color>\n- follows the Fusion connection to a room into a matching voice room", selecter: this, imageName: ImageName);
                if (recorder)
                {
                    XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[Recorder]</color>\n- manages the local user voice recording", associatedObject: recorder, imageName: ImageName, forceExitPrefabMode: true);
                    XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[MicrophonePermission]</color>\n- requires microphone access (for Android, visionOS, iOS, ...)", associatedObject: recorder, imageName: ImageName, forceExitPrefabMode: true); 
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

    }
    struct AddVoiceToNetworkRigXRAction : IXRAction, IXRActionSelecter
    {
        public int Weight => 10;
        public string Description => IsInstalled ? "Voice <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> voice";
        public string CategoryName => XRActionsManager.NETWORKRIG_CATEGORY;
        public string ImageName => "xrguide-add-networkrigvoice";
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
                if (userSpawner.UserPrefab.GetComponentInChildren<VoiceNetworkObject>() && userSpawner.UserPrefab.GetComponentInChildren<Speaker>())
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
                Selection.activeObject = stage.FindComponentOfType<VoiceNetworkObject>().gameObject;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                VoiceHelpersAutomation.AddVoiceToNetworkRigHeadset();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[VoiceNetworkObject and speaker]</color> to the network rig's headset\n- ensures to receive the voice for this user", selecter: this, imageName: ImageName);
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[AudioSource]</color> to the network rig's headset\n- includes spatial blend setting to 1, to ensure the voice is spatialized", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }

    struct AddBasicLipsyncToNetworkRigXRAction : IXRAction, IXRActionSelecter
    {
        public int Weight => 110;
        public string Description => IsInstalled ? "Basic lipsync <color=#A7A7A7>installed</color>" : "<color=#A7A7A7>Add</color> basic lipsync";
        public string CategoryName => XRActionsManager.NETWORKRIG_CATEGORY;
        public string ImageName => "xrguide-add-networkrigbasiclipsync"; 
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
                if (userSpawner.UserPrefab.GetComponentInChildren<SimpleLipsync>())
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
                Selection.activeObject = stage.FindComponentOfType<SimpleLipsync>().gameObject;
                return true;
            }
            return false;
        }

        public void Trigger()
        {
            if (TrySelect() == false)
            {
                VoiceHelpersAutomation.AddBasicLipsyncToNetworkRigHeadset();
                XRActionsManager.AddLog("Add <color=HIGHLIGHT_COLOR>[basic lipsync]</color> to the network rig's headset\n- randomly displays a mouth renderer among a list when voice activity is detected", selecter: this, imageName: ImageName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }


    static AddVoiceToRunnerXRAction addVoiceToRunnerXRAction;
    static AddVoiceToNetworkRigXRAction addVoiceToNetworkRigXRAction;
    static AddBasicLipsyncToNetworkRigXRAction addBasicLipsyncToNetworkRigXRAction;

    static VoiceHelpersXRActions()
    {
        XRActionsManager.RegisterAction(addVoiceToRunnerXRAction);
        XRActionsManager.RegisterAction(addVoiceToNetworkRigXRAction);
        XRActionsManager.RegisterAction(addBasicLipsyncToNetworkRigXRAction);
    }
}
#endif