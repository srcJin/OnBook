using Fusion.XR.Shared.Rig;

namespace Fusion.XR.Shared
{
    [System.Flags]
    public enum FeedbackMode
    {
        None = 0,
        Audio = 1,
        Haptic = 2,
        AudioAndHaptic = Audio | Haptic,
    }

    public interface IFeedbackHandler: IAudioFeedbackHandler, IHapticFeedbackHandler
    {
        public void PlayAudioAndHapticFeeback(string audioType = null, float hapticAmplitude = USE_DEFAULT_VALUES, float hapticDuration = USE_DEFAULT_VALUES, HardwareHand hardwareHand = null, FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic, bool audioOverwrite = true);

        public void StopAudioAndHapticFeeback(HardwareHand hardwareHand = null);

    }

    public interface IAudioFeedbackHandler
    {
        public void PlayAudioFeeback(string audioType = null);
        public void PauseAudioFeeback();
        public void StopAudioFeeback();
        public bool IsAudioFeedbackIsPlaying();
    }

    public interface IHapticFeedbackHandler
    {
        public const float USE_DEFAULT_VALUES = -1;
        public void PlayHapticFeedback(float hapticAmplitude = USE_DEFAULT_VALUES, HardwareHand hardwareHand = null, float hapticDuration = USE_DEFAULT_VALUES);
        public void StopHapticFeedback(HardwareHand hardwareHand = null);
    }
}
