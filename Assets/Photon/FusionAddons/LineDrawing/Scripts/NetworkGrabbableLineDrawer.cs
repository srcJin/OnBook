using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fusion.Addons.LineDrawing

{
    public class NetworkGrabbableLineDrawer : NetworkLineDrawer
    {
        [Header("Drawing input")]
        [SerializeField] InputActionProperty leftTriggerAction = new InputActionProperty(new InputAction());
        [SerializeField] InputActionProperty rightTriggerAction = new InputActionProperty(new InputAction());
        NetworkGrabbable grabbable;
        public bool IsGrabbed => grabbable && grabbable.IsGrabbed;

        protected IFeedbackHandler feedback;

        [Header("Feedback")]
        [SerializeField] string audioType;
        [SerializeField] float hapticAmplitudeFactor = 0.1f;
        [SerializeField] FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic;

        public InputActionProperty? CurrentInput { 
            get
            {
                if (grabbable == null || grabbable.CurrentGrabber == null || grabbable.CurrentGrabber.hand == null)
                {
                    return null;
                }
                return (grabbable.CurrentGrabber.hand.side == RigPart.LeftController) ? leftTriggerAction : rightTriggerAction;
            }
        }

        public float Pressure { 
            get
            {
                var input = CurrentInput;
                if (input == null) return 0;
                return input?.action.ReadValue<float>() ?? 0;
            }
        } 

        protected override void Awake()
        {
            base.Awake();
            leftTriggerAction.EnableWithDefaultXRBindings(side: RigPart.LeftController, new List<string> { "trigger" });
            rightTriggerAction.EnableWithDefaultXRBindings(side: RigPart.RightController, new List<string> { "trigger" });
            grabbable = GetComponentInChildren<NetworkGrabbable>();
            feedback = GetComponent<IFeedbackHandler>();
        }

        public override void Render()
        {
            base.Render();
            if (Object.HasStateAuthority)
            {
                VolumeDrawing();
            }
        }

        void VolumeDrawing()
        {
            var pressure = Pressure;
            if (pressure > 0.01f)
            {
                AddPoint(pressure: pressure);
                if (feedback != null)
                {
                    feedback.PlayAudioAndHapticFeeback(audioType: audioType, audioOverwrite: false, hapticAmplitude: Mathf.Clamp01(pressure * hapticAmplitudeFactor), feedbackMode: feedbackMode);
                }
            }
            else if(IsDrawingLine)
            {
                StopLine();
                if (feedback != null)
                {
                    feedback.StopAudioFeeback();
                }
            }

            if (IsGrabbed == false && currentDrawing != null)
            {
                StopDrawing();
                if (feedback != null)
                {
                    feedback.StopAudioFeeback();
                }
            }
        }
    }
}
