#if FUSION_WEAVER
using Fusion;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Fusion.Addons.VoiceHelpers
{
    [DefaultExecutionOrder(-10_000)]
    [RequireComponent(typeof(NetworkRunner))]
    public class FusionVoiceSetup : MonoBehaviour
    {
        public Recorder recorder;
        FusionVoiceClient fusionVoiceClient;
        MicrophonePermission microphonePermission;

        [Header("Permission callbacks")]
        public List<MonoBehaviour> enableOnRequestAnswered = new List<MonoBehaviour>();
        public UnityEvent onRequestAnswered;

        private void Awake()
        {
            fusionVoiceClient = GetComponent<FusionVoiceClient>();
            if (fusionVoiceClient == null)
            {
                fusionVoiceClient = gameObject.AddComponent<FusionVoiceClient>();
            }
            if (recorder == null)
            {
                recorder = GetComponentInChildren<Recorder>();
            }
            if (recorder == null)
            {
                var recorderGO = new GameObject("Recorder");
                recorderGO.transform.parent = transform;
                recorderGO.transform.position = transform.position;
                recorder = recorderGO.AddComponent<Recorder>();
                recorder.MicrophoneType = Recorder.MicType.Photon;
                recorder.FrameDuration = global::Photon.Voice.OpusCodec.FrameDuration.Frame60ms;
                recorder.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling48000;
            }
            fusionVoiceClient.PrimaryRecorder = recorder;
            microphonePermission = recorder.GetComponent<MicrophonePermission>();
            if (microphonePermission == null)
            {
                microphonePermission = recorder.gameObject.AddComponent<MicrophonePermission>();
            }
            MicrophonePermission.MicrophonePermissionCallback += OnMicrophonePermissionChange;
        }

        private void OnMicrophonePermissionChange(bool hasPermission)
        {
            foreach (var behaviour in enableOnRequestAnswered)
            {
                if (behaviour.enabled == false) behaviour.enabled = true;
            }
            if (onRequestAnswered != null) onRequestAnswered.Invoke();
        }
    }

}
#endif