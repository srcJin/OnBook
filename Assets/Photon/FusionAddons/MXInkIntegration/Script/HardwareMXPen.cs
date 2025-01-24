using Fusion.XR.Shared;
using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Fusion.Addons.MXPen
{
    public class HardwareMXPen : VrStylusHandler
    {
        RigLocomotion rigLocomotion;

#if UNITY_EDITOR
        [Header("Editor debugging")]
        [Tooltip("Always consider the right controller is a MX Ink")]
        public bool forceRightHandPen = true;
        [Tooltip("If the actual device is not a MX pen, check this to avoid having draw it all the time (the MX pen tip pressure set to 0 is equivalent to a stick axis always set to the right)")]
        public bool forcedHandPenIsAController = false;
        bool isDeviceFound = false;

        UnityEngine.XR.InputDevice inputDevice;
        InputActionProperty rightGrabAction = new InputActionProperty(new InputAction());
        InputActionProperty rightTriggerAction = new InputActionProperty(new InputAction());
        InputActionProperty rightControllerTurnAction = new InputActionProperty(new InputAction());
        InputActionProperty rightAButton = new InputActionProperty(new InputAction());
#endif

        private void Awake()
        {
            rigLocomotion = GetComponentInParent<RigLocomotion>();
#if UNITY_EDITOR
            rightTriggerAction.EnableWithDefaultXRBindings(side: RigPart.RightController, new List<string> { "trigger" });
            rightGrabAction.EnableWithDefaultXRBindings(side: RigPart.RightController, new List<string> { "grip" });
            rightControllerTurnAction.EnableWithDefaultXRBindings(side: RigPart.RightController, new List<string> { "joystick" });
            rightAButton.EnableWithDefaultXRBindings(side: RigPart.RightController, new List<string> { "primaryButton" });
#endif

        }

#if UNITY_EDITOR
        void FindForcedPenHand()
        {
            if (isDeviceFound) return;
            InputDeviceCharacteristics desiredCharacteristics = InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right;
            var devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, devices);
            foreach (var d in devices)
            {
                inputDevice = d;
                isDeviceFound = true;
                break;
            }
        }
#endif

#if UNITY_EDITOR
        protected override void Update()
        {
            if (forceRightHandPen)
            {
                FindForcedPenHand();
                _right_touch_controller.SetActive(false);
                _left_touch_controller.SetActive(true);
                _stylus.isActive = true;
                _stylus.isOnRightHand = true;
                _stylus.cluster_middle_value = rightTriggerAction.action.ReadValue<float>();
                if (forcedHandPenIsAController)
                {
                    // Force end pen is in fact a controller: tweak the input to avoid drawing all the time
                    _stylus.tip_value = rightControllerTurnAction.action.ReadValue<Vector2>().x;
                }
                else
                {
                    // Force end pen is a MXInk stylus: use the expected inputstylus
                    _stylus.tip_value = 1 - rightControllerTurnAction.action.ReadValue<Vector2>().x;
                }
                _stylus.cluster_front_value = rightGrabAction.action.ReadValue<float>() > 0.5f;
                _stylus.cluster_back_value = rightAButton.action.ReadValue<float>() > 0.5f;
                _mxInk_model.SetActive(true);
                if (isDeviceFound && inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation))
                {
                    transform.localRotation = rotation;
                }
                if (isDeviceFound && inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position))
                {
                    transform.localPosition = position;
                }
            }
            else
            {
                base.Update();
            }
        }
#endif

        private void LateUpdate()
        {
            if (rigLocomotion != null)
            {
                rigLocomotion.disableRightHandRotation = CurrentState.isActive && CurrentState.isOnRightHand;
                rigLocomotion.disableLeftHandRotation = CurrentState.isActive && CurrentState.isOnRightHand == false;
            }
        }
    }
}
