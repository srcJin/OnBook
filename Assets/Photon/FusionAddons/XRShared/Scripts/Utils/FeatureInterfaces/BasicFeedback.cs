using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Fusion.XR.Shared
{
    /***
     * 
     * BasicFeedback manages the audio and haptic feedbacks
     * It provides methods to :
     *  - start/pause/stop playing audio feedback only
     *  - start playing audio and haptic feeback in the same time
     * 
     * See the Feedback add-on for a full-fledge implementation supporting a centralized audio source and a centralized sound list
     ***/



    public class BasicFeedback : MonoBehaviour, IFeedbackHandler
    {

        [System.Serializable]
        public struct AudioFeedbackEntry
        {
            public string audioType;
            public AudioClip clip;
        }

        [SerializeField] List<AudioFeedbackEntry> audioLibrary = new List<AudioFeedbackEntry>();
        Dictionary<string, AudioFeedbackEntry> audioEntryByType = new Dictionary<string, AudioFeedbackEntry>();

        public bool EnableAudioFeedback = true;
        public bool EnableHapticFeedback = true;

        public AudioSource audioSource;

        [Header("Haptic feedback")]
        public float defaultHapticAmplitude = 0.4f;
        public float defaultHapticDuration = 0.1f;

        NetworkGrabbable grabbable;
        public virtual bool IsGrabbed => grabbable.IsGrabbed;
        public virtual bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable.CurrentGrabber.Object.StateAuthority == grabbable.CurrentGrabber.Object.Runner.LocalPlayer;

        protected virtual void Awake()
        {
            grabbable = GetComponent<NetworkGrabbable>();
            FillCache();
        }

        void Start()
        {
            // Note: using many Audiosource in a single scene is problematic in Unity: See the Feedback add-on for a full-fledge implementation supporting a centralized audio source
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                Debug.LogError("No audio source found for BasicFeedback");
        }

        void FillCache()
        {
            audioEntryByType.Clear();
            foreach (var entry in audioLibrary)
            {
                audioEntryByType[entry.audioType] = entry;
            }
        }

        #region IAudioFeedbackHandler
        public void PlayAudioFeeback(string audioType = null)
        {
            if (audioSource == null || EnableAudioFeedback == false) return;

            AudioClip targetClip = null;
            if (audioType != null && audioEntryByType.ContainsKey(audioType))
            {
                targetClip = audioEntryByType[audioType].clip;
            }
            if (targetClip != null)
            {
                audioSource.clip = targetClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning($"AudioType {audioType} not found");
            }
        }

        public void StopAudioFeeback()
        {
            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();
        }

        public void PauseAudioFeeback()
        {
            if (audioSource && audioSource.isPlaying)
                audioSource.Pause();
        }
        public bool IsAudioFeedbackIsPlaying()
        {
            return audioSource && audioSource.isPlaying;
        }
        #endregion

        #region IHapticFeedbackHandler


        public virtual void PlayHapticFeedback(float hapticAmplitude = IFeedbackHandler.USE_DEFAULT_VALUES, HardwareHand hardwareHand = null, float hapticDuration = IFeedbackHandler.USE_DEFAULT_VALUES)
        {
            if (hapticAmplitude == IFeedbackHandler.USE_DEFAULT_VALUES) hapticAmplitude = defaultHapticAmplitude;
            if (hapticDuration == IFeedbackHandler.USE_DEFAULT_VALUES) hapticDuration = defaultHapticDuration;
            if (hardwareHand == null)
            {
                hardwareHand = GrabbingHand();

            }
            if (EnableHapticFeedback == false || hardwareHand == null) return;
            hardwareHand.SendHapticImpulse(amplitude: hapticAmplitude, duration: hapticDuration);
        }

        public void StopHapticFeedback(HardwareHand hardwareHand = null)
        {
            if (hardwareHand == null) return;

            hardwareHand.StopHaptics();
        }
        #endregion

        #region IFeedbackHandler

        public void PlayAudioAndHapticFeeback(string audioType = null, float hapticAmplitude = -1, float hapticDuration = -1, HardwareHand hardwareHand = null, FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic, bool audioOverwrite = true)
        {
            if ((feedbackMode & FeedbackMode.Audio) != 0)
            {
                if (IsAudioFeedbackIsPlaying() == false || audioOverwrite == true)
                    PlayAudioFeeback(audioType);
            }

            if ((feedbackMode & FeedbackMode.Haptic) != 0)
            {
                PlayHapticFeedback(hapticAmplitude, hardwareHand, hapticDuration);
            }
        }

        public void StopAudioAndHapticFeeback(HardwareHand hardwareHand = null)
        {
            StopAudioFeeback();
            StopHapticFeedback(hardwareHand);
        }
        #endregion

        HardwareHand GrabbingHand()
        {
            if (grabbable != null)
            {
                if (IsGrabbedByLocalPLayer && grabbable.CurrentGrabber.hand && grabbable.CurrentGrabber.hand.LocalHardwareHand != null)
                {
                    return grabbable.CurrentGrabber.hand.LocalHardwareHand;
                }
            }
            return null;
        }
    }
}