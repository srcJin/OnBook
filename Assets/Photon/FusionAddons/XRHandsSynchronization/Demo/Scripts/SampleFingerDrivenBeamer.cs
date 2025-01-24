using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Fusion.Addons.XRHandsSync.Demo
{
    public class SampleFingerDrivenBeamer : MonoBehaviour
    {
        HardwareHand hardwareHand;
        HardwareRig hardwareRig;
        RayBeamer beamer;
        Pose rayDefaultPose;
        bool beamerDefaultUseRayActionInput = false;
        bool areInputControlled = false;

        [SerializeField]
        float maxThumbIndexAngleForBeam = 20;
        [SerializeField]
        float poseDurationBeforeBeamActivation = 0.5f;
        float wasActiveStart = -1;
        bool wasActive = false;

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
            hardwareRig = GetComponentInParent<HardwareRig>();
            beamer = GetComponentInParent<RayBeamer>();
            if (hardwareHand == null && hardwareRig == null)
                Debug.LogError("Missing hardware rig parts");
            if (beamer == null)
                Debug.LogError("Missing beamer");
            beamerDefaultUseRayActionInput = beamer.useRayActionInput;
            rayDefaultPose = new Pose(
                hardwareHand.transform.InverseTransformPoint(beamer.origin.position),
                Quaternion.Inverse(hardwareHand.transform.rotation) * beamer.origin.rotation
            );
        }

        private void OnDisable()
        {
            DesactivateInputControl();
        }

        void ActivateInputControl()
        {
            if (areInputControlled) return;
            areInputControlled = true;
            beamer.useRayActionInput = false;
        }

        void DesactivateInputControl()
        {
            if (areInputControlled == false) return;
            areInputControlled = false;
            if (wasActive)
            {
                beamer.CancelHit();
            }
            beamer.useRayActionInput = beamerDefaultUseRayActionInput;
        }

        float middleFingerClosedThreshold = 0.05f;
        float ringFingerClosedThreshold = 0.05f;
        float littleFingerClosedThreshold = 0.05f;
        private void UpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags succesFlags, XRHandSubsystem.UpdateType updateType)
        {
            if (enabled == false || gameObject.activeSelf == false) return;
            switch (updateType)
            {
                case XRHandSubsystem.UpdateType.Dynamic:
                    bool actionDetected = false;
                    Pose beamerPose = rayDefaultPose;
                    bool poseAnalysed = false;

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
                        var indexTipJoint = hand.GetJoint(XRHandJointID.IndexTip);
                        var indexDistalJoint = hand.GetJoint(XRHandJointID.IndexDistal);
                        var indexIntermediateJoint = hand.GetJoint(XRHandJointID.IndexIntermediate);
                        var indexProximalJoint = hand.GetJoint(XRHandJointID.IndexProximal);
                        var indexMetacarpalJoint = hand.GetJoint(XRHandJointID.IndexMetacarpal);
                        var thumbDistalJoint = hand.GetJoint(XRHandJointID.ThumbDistal);
                        var thumbProximalJoint = hand.GetJoint(XRHandJointID.ThumbProximal);
                        var thumbMetacarpalJoint = hand.GetJoint(XRHandJointID.ThumbMetacarpal);


                        var indexTipAvailable = indexTipJoint.TryGetPose(out var indexTipPose);
                        var indexDistalAvailable = indexDistalJoint.TryGetPose(out var indexDistalPose);
                        var indexIntermediatePoseAvailable = indexIntermediateJoint.TryGetPose(out var indexIntermediatePose);
                        var indexProximalAvailable = indexProximalJoint.TryGetPose(out var indexProximalPose);
                        var indexMetacarpalAvailable = indexMetacarpalJoint.TryGetPose(out var indexMetacarpalPose);
                        var thumbDistalAvailable = thumbDistalJoint.TryGetPose(out var thumbDistalPose);
                        var thumbProximalAvailable = thumbProximalJoint.TryGetPose(out var thumbProximalPose);
                        var thumbMetacarpalAvailable = thumbMetacarpalJoint.TryGetPose(out var thumbMetacarpalPose);

                        // Index
                        if (indexTipAvailable && indexDistalAvailable && indexIntermediatePoseAvailable && indexProximalAvailable && thumbDistalAvailable && thumbProximalAvailable && indexMetacarpalAvailable && thumbMetacarpalAvailable)
                        {
                            poseAnalysed = true;
                            var baseThumb = Quaternion.Inverse(thumbMetacarpalPose.rotation);
                            var baseindex = Quaternion.Inverse(indexMetacarpalPose.rotation);
                            bool indexPoseOk = false;
                            bool thumbPoseOk = false;
                            if (
                                NormalisedAngle((baseindex * indexProximalPose.rotation).eulerAngles.x) < maxThumbIndexAngleForBeam
                                && NormalisedAngle((baseindex * indexIntermediatePose.rotation).eulerAngles.x) < maxThumbIndexAngleForBeam
                                && NormalisedAngle((baseindex * indexDistalPose.rotation).eulerAngles.x) < maxThumbIndexAngleForBeam
        )
                            {
                                indexPoseOk = true; ;
                            }
                            if (
                                NormalisedAngle((baseThumb * thumbProximalPose.rotation).eulerAngles.x) < maxThumbIndexAngleForBeam
                                && NormalisedAngle((baseThumb * thumbDistalPose.rotation).eulerAngles.x) < maxThumbIndexAngleForBeam
                                )
                            {
                                thumbPoseOk = true;
                            }

                            if (thumbPoseOk && (indexPoseOk || wasActive))
                            {

                                actionDetected = true;
                                beamerPose = indexMetacarpalPose;
                            }
                        }
                    }
                    else
                    {
                        DesactivateInputControl();
                    }

                    if (poseAnalysed == false && wasActive)
                    {
                        beamer.CancelHit();
                    }

                    if (actionDetected && wasActiveStart == -1)
                    {
                        // To start the pose, we check that the other parts of the hand are closed

                        var middleJoint = hand.GetJoint(XRHandJointID.MiddleTip);
                        var ringJoint = hand.GetJoint(XRHandJointID.RingTip);
                        var littleJoint = hand.GetJoint(XRHandJointID.LittleTip);
                        var palmJoint = hand.GetJoint(XRHandJointID.Palm);

                        // Grab
                        if (middleJoint.TryGetPose(out var middlePose) && ringJoint.TryGetPose(out var ringPose) && palmJoint.TryGetPose(out var palmPose) && littleJoint.TryGetPose(out var littlePose))
                        {
                            var distanceMiddle = Vector3.Distance(middlePose.position, palmPose.position);
                            var distanceRing = Vector3.Distance(ringPose.position, palmPose.position);
                            var distanceLittle = Vector3.Distance(littlePose.position, palmPose.position);
                            if (distanceMiddle > middleFingerClosedThreshold || distanceRing > ringFingerClosedThreshold || distanceLittle > littleFingerClosedThreshold)
                            {
                                actionDetected = false;
                            }
                        }
                        else
                        {
                            actionDetected = false;
                        }
                    }

                    bool isBeamActive = false;
                    if (actionDetected)
                    {
                        if (poseDurationBeforeBeamActivation == 0)
                        {
                            isBeamActive = true;
                        }
                        else if (wasActiveStart != -1)
                        {
                            if ((Time.time - wasActiveStart) > poseDurationBeforeBeamActivation)
                            {
                                isBeamActive = true;
                            }
                        }
                    }

                    if (isBeamActive)
                    {
                        beamer.isRayEnabled = true;
                        // Pose provided by XRHands are relative to the rig
                        beamer.origin.position = hardwareRig.transform.TransformPoint(beamerPose.position);
                        beamer.origin.rotation = hardwareRig.transform.rotation * beamerPose.rotation;
                    }
                    else
                    {
                        beamer.isRayEnabled = false;
                        // Pose was captured relatively to the hand
                        beamer.origin.position = hardwareHand.transform.TransformPoint(rayDefaultPose.position);
                        beamer.origin.rotation = hardwareHand.transform.rotation * rayDefaultPose.rotation;
                    }
                    wasActive = isBeamActive;

                    if (actionDetected)
                    {
                        if (wasActiveStart == -1)
                        {
                            wasActiveStart = Time.time;
                        }
                    }
                    else
                    {
                        wasActiveStart = -1;
                    }
                    break;
            }

            float NormalisedAngle(float angle)
            {
                return Mathf.Repeat(angle + 180f, 360f) - 180f;
            }
        }
    }

}
