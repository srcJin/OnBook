using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Fusion.Addons.XRHandsSync.Demo
{
    public class SampleFingerDrivenControllerInput : MonoBehaviour
    {
        HardwareHand hardwareHand;
        bool hardwareHandDefaultUpdateHandCommandWithAction = false;
        bool hardwareHandDefaultUpdateGrabWithAction = false;
        bool areInputControlled = false;

        private void Start()
        {
            XRHandSubsystem m_Subsystem =
                XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();

            if (m_Subsystem != null) m_Subsystem.updatedHands += UpdatedHands;
        }

        private void OnDestroy()
        {
            XRHandSubsystem m_Subsystem =
                XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();

            if (m_Subsystem != null) m_Subsystem.updatedHands -= UpdatedHands;
        }

        private void Awake()
        {
            hardwareHand = GetComponentInParent<HardwareHand>();
            if (hardwareHand == null)
                Debug.LogError("Missing hardware hand");
            hardwareHandDefaultUpdateHandCommandWithAction = hardwareHand.updateHandCommandWithAction;
            hardwareHandDefaultUpdateGrabWithAction = hardwareHand.updateGrabWithAction;

        }

        void ActivateInputControl()
        {
            if (areInputControlled) return;
            areInputControlled = true;
            hardwareHand.updateHandCommandWithAction = false;
            hardwareHand.updateGrabWithAction = false;
        }

        private void OnDisable()
        {
            DesactivateInputControl();
        }

        void DesactivateInputControl()
        {
            if (areInputControlled == false) return;
            areInputControlled = false;
            hardwareHand.updateHandCommandWithAction = hardwareHandDefaultUpdateHandCommandWithAction;
            hardwareHand.updateGrabWithAction = hardwareHandDefaultUpdateGrabWithAction;
        }

        float pinchThreshold = 0.02f;
        float middleFingerGrabThreshold = 0.05f;
        float ringFingerGrabThreshold = 0.05f;
        float indexFingerPressedThreshold = 0.05f;
        float thumFingerPressedThreshold = 0.05f;

        private void UpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags succesFlags, XRHandSubsystem.UpdateType updateType)
        {
            if (enabled == false || gameObject.activeSelf == false) return;
            switch (updateType)
            {
                case XRHandSubsystem.UpdateType.Dynamic:
                    XRHandSubsystem.UpdateSuccessFlags pose;
                    XRHand hand;
                    if (hardwareHand.side == RigPart.LeftController)
                    {
                        pose = XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;
                        hand = subsystem.leftHand;
                    }
                    else
                    {
                        pose = XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;
                        hand = subsystem.rightHand;
                    }
                    var handavailable = (succesFlags & pose) != 0;
                    if (handavailable)
                    {
                        ActivateInputControl();
                        hardwareHand.handCommand.pinchCommand = 0;
                        hardwareHand.handCommand.gripCommand = 0;
                        hardwareHand.handCommand.triggerCommand = 0;
                        hardwareHand.handCommand.thumbTouchedCommand = 0;
                        hardwareHand.isGrabbing = false;
                        var indexJoint = hand.GetJoint(XRHandJointID.IndexTip);
                        var thumbJoint = hand.GetJoint(XRHandJointID.ThumbTip);
                        var middleJoint = hand.GetJoint(XRHandJointID.MiddleTip);
                        var ringJoint = hand.GetJoint(XRHandJointID.RingTip);
                        var palmJoint = hand.GetJoint(XRHandJointID.Palm);
                        var indexIntermediateJoint = hand.GetJoint(XRHandJointID.IndexIntermediate);
                        var indexProximalJoint = hand.GetJoint(XRHandJointID.IndexProximal);


                        var indexAvailable = indexJoint.TryGetPose(out var indexPose);
                        var thumbAvailable = thumbJoint.TryGetPose(out var thumbPose);
                        var palmPoseAvailable = palmJoint.TryGetPose(out var palmPose);
                        var indexIntermediatePoseAvailable = indexIntermediateJoint.TryGetPose(out var indexIntermediatePose);

                        // Index
                        if (indexAvailable && palmPoseAvailable)
                        {
                            var distance = Vector3.Distance(indexPose.position, palmPose.position);
                            if (distance < indexFingerPressedThreshold)
                            {
                                hardwareHand.handCommand.triggerCommand = 1;
                            }
                        }
                        // Thumb
                        if (thumbAvailable && indexIntermediatePoseAvailable)
                        {
                            var distance = Vector3.Distance(thumbPose.position, indexIntermediatePose.position);
                            if (distance < thumFingerPressedThreshold)
                            {
                                hardwareHand.handCommand.thumbTouchedCommand = 1;
                            }
                        }
                        if (hardwareHand.handCommand.thumbTouchedCommand == 0)
                        {
                            var indexProximalPoseAvailable = indexProximalJoint.TryGetPose(out var indexProximalPose);
                            if (thumbAvailable && indexProximalPoseAvailable)
                            {
                                var distance = Vector3.Distance(thumbPose.position, indexProximalPose.position);
                                if (distance < thumFingerPressedThreshold)
                                {
                                    hardwareHand.handCommand.thumbTouchedCommand = 1;
                                }
                            }
                        }

                        // Pinch
                        if (indexAvailable && thumbAvailable)
                        {
                            var distance = Vector3.Distance(indexPose.position, thumbPose.position);
                            if (distance < pinchThreshold)
                            {
                                hardwareHand.handCommand.pinchCommand = 1;
                                //hardwareHand.handCommand.thumbTouchedCommand = 1;
                                //hardwareHand.handCommand.triggerCommand = 0.6f;
                                hardwareHand.isGrabbing = true;
                            }
                        }

                        // Grab
                        if (middleJoint.TryGetPose(out var middlePose) && ringJoint.TryGetPose(out var ringPose) && palmPoseAvailable)
                        {
                            var distanceMiddle = Vector3.Distance(middlePose.position, palmPose.position);
                            var distanceRing = Vector3.Distance(ringPose.position, palmPose.position);
                            if (distanceMiddle < middleFingerGrabThreshold && distanceRing < ringFingerGrabThreshold)
                            {
                                hardwareHand.handCommand.gripCommand = 1;
                                hardwareHand.isGrabbing = true;
                            }
                        }
                    }
                    else
                    {
                        DesactivateInputControl();
                    }
                    break;
            }
        }
    }
}
