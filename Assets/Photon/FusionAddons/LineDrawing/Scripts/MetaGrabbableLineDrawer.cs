using Fusion.XR.Shared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fusion.Addons.LineDrawing.Meta

{
    public class MetaGrabbableLineDrawer : NetworkLineDrawer
    {
#if OCULUS_SDK_AVAILABLE
#if OCULUS_INTERACTION_SDK_AVAILABLE
        InputActionProperty leftTriggerAction = new InputActionProperty(new InputAction());
        InputActionProperty rightTriggerAction = new InputActionProperty(new InputAction());
        Oculus.Interaction.Grabbable metaGrabbable;
        public bool isGrabbed = false;
        public Oculus.Interaction.Input.Controller grabbingController = null;
        public Oculus.Interaction.GrabInteractor grabbingInteractor = null;
        public InputActionProperty? CurrentInput
        {
            get
            {
                if (grabbingController == null)
                {
                    return null;
                }
                return (grabbingController?.Handedness == Oculus.Interaction.Input.Handedness.Left) ? leftTriggerAction : rightTriggerAction;
            }
        }

        public float Pressure
        {
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
            leftTriggerAction.EnableWithDefaultXRBindings(leftBindings: new List<string> { "trigger" });
            rightTriggerAction.EnableWithDefaultXRBindings(rightBindings: new List<string> { "trigger" });
            if (metaGrabbable == null) metaGrabbable = GetComponentInChildren<Oculus.Interaction.Grabbable>();
        }
        private void OnEnable()
        {
            metaGrabbable.WhenPointerEventRaised += OnPointerEvent;
        }

        private void OnDisable()
        {
            metaGrabbable.WhenPointerEventRaised -= OnPointerEvent;
        }

        private void OnPointerEvent(Oculus.Interaction.PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case Oculus.Interaction.PointerEventType.Select:
                    isGrabbed = true;
                    if (pointerEvent.Data is Oculus.Interaction.GrabInteractor interactor)
                    {
                        grabbingInteractor = interactor;
                        grabbingController = interactor.GetComponentInParent<Oculus.Interaction.Input.Controller>();
                    }
                    break;
                case Oculus.Interaction.PointerEventType.Unselect:
                    isGrabbed = false;
                    grabbingController = null;
                    break;
                default:
                    return;
            }
        }

        public override void Render()
        {
            base.Render();
            if (Object.HasStateAuthority)
            {
                // TODO: Extrapolation
                    
                VolumeDrawing();
            }
        }

        void VolumeDrawing()
        {
            var pressure = Pressure;
            if (pressure > 0.01f)
            {
                AddPoint(pressure: pressure);
            }
            else
            {
                StopLine();
            }

            if (isGrabbed == false && currentDrawing != null)
            {
                StopDrawing();
            }
        }
#endif
#endif
    }
}
