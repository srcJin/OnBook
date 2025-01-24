using Photon.Voice;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * The SimpleLipsync OnAudioFilterRead compute an average voiceVolume for data received. It is used to animate avatar mouth if voice volume exceed a specific threshold.
 * 
 * Note: not compatible with webGL, as OnAudioFilterData is not called on this platform. Use Speaker.RemoteVoice.FloatFrameDecoded instead. See Avatar add-on for a webGL compatible voice detection.
 **/

namespace Fusion.Addons.VoiceHelpers
{
    public class SimpleLipsync : MonoBehaviour
    {
        public float voiceVolume = 0;
        [Tooltip("A set of renderers displaying possible state of the mouth. one will be picked randomly while speaking")]
        public List<Renderer> speakingRenderers = new List<Renderer>();
        public Renderer mutedRender;
        public float mouthRefreshDelay = 0.1f;
        public float speakThreshold = 0.001f;

        AudioSource audioSource;
        private bool isMuted = true;
        private float nextMouthUpdate = 0;

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }


        protected virtual void Update()
        {
            // reset the voice volume to stop lip sync when user exit a chat bubble
            if (audioSource && !audioSource.enabled)
            {
                voiceVolume = 0;
            }
            // Mouth animaton
            if (nextMouthUpdate < Time.time)
            {
                UpdateMouth();
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            voiceVolume = 0f;
            foreach (var sample in data)
            {
                voiceVolume += Mathf.Abs(sample);
            }
            voiceVolume /= data.Length;
        }

        #region Mouth

        // UpdateMouth animates the avatar mouth according to voice detection
        // The mouth movement is random
        protected virtual void UpdateMouth()
        {
            nextMouthUpdate = Time.time + mouthRefreshDelay;

            if (voiceVolume < speakThreshold)
            {
                if (!isMuted)
                {
                    CloseMouth();
                }
                isMuted = true;
            }
            else
            {
                if (isMuted)
                {
                    mutedRender.enabled = false;
                }
                isMuted = false;
                ShowMouthStep(Random.Range(0, speakingRenderers.Count));
            }
        }

        // CloseMouth displays the muted mouth renderer and hide others speaking mouth renderers
        protected virtual void CloseMouth()
        {
            if(mutedRender) mutedRender.enabled = true;
            ShowMouthStep(-1);
        }

        // ShowMouthStep displays only one speaking mouth renderer
        protected virtual void ShowMouthStep(int index)
        {
            int i = 0;
            foreach (var renderer in speakingRenderers)
            {
                renderer.enabled = i == index;
                i++;
            }
        }
        #endregion
    }
}
