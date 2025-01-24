#if XRSHARED_ADDON_AVAILABLE
using Fusion.Addons.MXPen;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.MXPenIntegration
{
    [RequireComponent(typeof(NetworkMXPen))]
    public class MXPenFeedback : BasicFeedback
    {
#if OCULUS_SDK_AVAILABLE
        public override bool IsGrabbed => true;
        public override bool IsGrabbedByLocalPLayer => IsGrabbed && networkMXPen != null && networkMXPen.Object.StateAuthority == networkMXPen.Object.Runner.LocalPlayer;

        NetworkMXPen networkMXPen;

#if UNITY_EDITOR
        HardwareRig hardwareRig;
#endif
        protected override void Awake()
        {
            base.Awake();
            networkMXPen = GetComponent<NetworkMXPen>();
        }

        private void Reset()
        {
            // Change default vibration duration at first install of the component
            defaultHapticDuration = 0.01f;
        }

        public override void PlayHapticFeedback(float hapticAmplitude = IFeedbackHandler.USE_DEFAULT_VALUES, HardwareHand hardwareHand = null, float hapticDuration = IFeedbackHandler.USE_DEFAULT_VALUES)
        {
            if (hapticAmplitude == IFeedbackHandler.USE_DEFAULT_VALUES) hapticAmplitude = defaultHapticAmplitude;
            if (hapticDuration == IFeedbackHandler.USE_DEFAULT_VALUES) hapticDuration = defaultHapticDuration;

            if (EnableHapticFeedback == false) return;

#if UNITY_EDITOR
            if (networkMXPen.LocalHardwareStylus && networkMXPen.LocalHardwareStylus is HardwareMXPen localPen)
            {
                if(localPen.forceRightHandPen && localPen.forcedHandPenIsAController)
                {
                    if(hardwareRig == null)
                    {
                        hardwareRig = FindObjectOfType<HardwareRig>();
                    }
                    if (hardwareRig) {
                        base.PlayHapticFeedback(hapticAmplitude, hardwareRig.rightHand, hapticDuration);
                        return;
                    }
                }
            }
#endif

            if (networkMXPen.LocalHardwareStylus && networkMXPen.LocalHardwareStylus.CurrentState.isOnRightHand)
            {
                OVRPlugin.TriggerVibrationAction("haptic_pulse", OVRPlugin.Hand.HandRight, hapticDuration, hapticAmplitude);
            } 
            else
            {
                OVRPlugin.TriggerVibrationAction("haptic_pulse", OVRPlugin.Hand.HandLeft, hapticDuration, hapticAmplitude);

            }
        }
#endif
    }
}
#endif