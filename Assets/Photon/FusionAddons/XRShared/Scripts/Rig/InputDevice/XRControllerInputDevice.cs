using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Detect and synchronize XR device position with gamobject transform
/// 
/// Note: compatible with Unity 2020 and more. For Unity 2019, InputDeviceRole should be used instead of InputDeviceCharacteristics
/// </summary>
namespace Fusion.XR.Shared.Rig
{
    public class XRControllerInputDevice : XRInputDevice
    {
        static Vector3 defaultLeftPosOffset = new Vector3(0.03f, 0.01f, -0.03f);
        static Vector3 defaultLeftRotOffset = new Vector3(-45, 0, 0);
        static Vector3 defaultRightPosOffset = new Vector3(-0.03f, 0.01f, -0.03f);
        static Vector3 defaultRightRotOffset = new Vector3(-45, 0, 0);
        public Vector3 occulusPluginPositionOffset = defaultLeftPosOffset;
        public Vector3 occulusPluginRotationOffset = defaultLeftRotOffset;

        public enum ControllerSide
        {
            Left,
            Right
        }

        [Header("Hand type")]
        public ControllerSide side = ControllerSide.Right;

        protected override InputDeviceCharacteristics DesiredCharacteristics => InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice | (side == ControllerSide.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);

        private void Awake()
        {
            if (side == ControllerSide.Right && occulusPluginPositionOffset == defaultLeftPosOffset)
            {
                occulusPluginPositionOffset = defaultRightPosOffset;
            }
            if (side == ControllerSide.Right && occulusPluginRotationOffset == defaultLeftRotOffset)
            {
                occulusPluginRotationOffset = defaultRightRotOffset;
            }
        }

        override protected Vector3 AdaptPosition(Vector3 pos)
        {
            if (isUsingOculusPlugin)
            {
                pos += occulusPluginPositionOffset.x * transform.right + occulusPluginPositionOffset.y * transform.up + occulusPluginPositionOffset.z * transform.forward;
            }
            return pos;
        }

        override protected Quaternion AdaptRotation(Quaternion rot)
        {
            if (isUsingOculusPlugin)
            {
                rot = rot * Quaternion.Euler(occulusPluginRotationOffset);
            }
            return rot;
        }
    }
}
