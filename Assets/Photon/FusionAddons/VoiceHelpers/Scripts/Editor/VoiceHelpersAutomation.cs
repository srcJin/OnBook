#if FUSION_WEAVER
using Fusion.XR.Shared.Editor;
using Fusion.XR.Shared.Rig;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.VoiceHelpers.Editor
{
    public static class VoiceHelpersAutomation
    {
        const string recorderGuid = "48f2b4f0372ce6246bda69f78764f640";
        public const string headsetVoiceGuid = "aab71cb112738446bb67cf3803ae1c93";
        public const string mouthGuid = "4bd99d2e88e9bae429d7461ad5280d1d";
        static Vector3 MouthLocalPosition = new Vector3(0, -0.14f, 0.043f);

        public static bool IsVoiceOnRunner()
        {
            var userSpawner = XRSharedAutomation.CurrentUserSpawner() as MonoBehaviour;
            if (userSpawner == null) return false;
            var isVoiceClientOk = userSpawner.GetComponent<FusionVoiceClient>() || userSpawner.GetComponent<FusionVoiceSetup>();
            if (isVoiceClientOk && userSpawner.GetComponentInChildren<Recorder>() )
            {
                return true;
            }
            return false;
        }

        public static void AddVoiceToRunner(out Recorder recorder)
        {
            recorder = null;
            var userSpawner = XRSharedAutomation.CurrentUserSpawner() as MonoBehaviour;
            if (userSpawner == null)
            {
                Debug.LogError("Add a connection manager and a network rig set in it to the scene first");
                return;
            }

            if(XRProjectAutomation.SpawnChildIfnotPresent(userSpawner.gameObject, recorderGuid, out var recorderInstance) == false)
            {
                Debug.Log("Recorder already present");
            }

            if (recorderInstance)
            {
                recorder = recorderInstance.GetComponent<Recorder>();
            }

            if (userSpawner.GetComponent<FusionVoiceClient>() == null)
            {
                var voiceClient = userSpawner.gameObject.AddComponent<FusionVoiceClient>();
                if(recorder) voiceClient.PrimaryRecorder = recorder;
            }
        }

        public static void AddVoiceToNetworkRigHeadset()
        {
            string assetPath = XRSharedAutomation.CurrentUserSpawnerPrefabPath();
            if (assetPath == null)
            {
                Debug.LogError("Add a connection manager and a network rig set in it to the scene first");
                return;
            }

            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
            NetworkRig networkRig = contentsRoot.GetComponent<NetworkRig>();

            XRProjectAutomation.SpawnChildIfnotPresent(networkRig.headset, headsetVoiceGuid);

            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }

        public static void AddBasicLipsyncToNetworkRigHeadset()
        {
            string assetPath = XRSharedAutomation.CurrentUserSpawnerPrefabPath();
            if (assetPath == null)
            {
                Debug.LogError("Add a connection manager and a network rig set in it to the scene first");
                return;
            }

            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
            NetworkRig networkRig = contentsRoot.GetComponent<NetworkRig>();

            XRProjectAutomation.SpawnChildIfnotPresent(networkRig.headset, mouthGuid, out var mouth);
            mouth.transform.localPosition = MouthLocalPosition;
            var voiceNetworkObject = networkRig.headset.GetComponentInChildren<VoiceNetworkObject>();
            if (voiceNetworkObject == null)
            {
                XRProjectAutomation.SpawnChildIfnotPresent(networkRig.headset, headsetVoiceGuid, out var voiceInstance);
                voiceNetworkObject = voiceInstance.GetComponentInChildren<VoiceNetworkObject>();
            }
            if (voiceNetworkObject == null || mouth == null) throw new Exception("Unexpected error");
            if (voiceNetworkObject.GetComponent<SimpleLipsync>() == null)
            {
                var lipSync = voiceNetworkObject.gameObject.AddComponent<SimpleLipsync>();
                lipSync.speakingRenderers = new List<Renderer>();
                foreach(var renderer in mouth.GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.name.Contains("Muted", StringComparison.CurrentCultureIgnoreCase))
                    {
                        lipSync.mutedRender = renderer;
                    }
                    else
                    {
                        lipSync.speakingRenderers.Add(renderer);
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }

}
#endif