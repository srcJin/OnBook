using System.Collections;
using System.Collections.Generic;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Fusion.Addons.XRHandsSync
{
    public abstract class FingerDrivenGesture : MonoBehaviour
    {
        XRHandCollectableSkeletonDriver xrHandsCollectableSkeletonDriver;
        protected HardwareHand hardwareHand;
        protected HardwareRig hardwareRig;
        protected bool areInputControlled = false;
        protected XRHand lastXRHand;

        [SerializeField] bool disableGrabIfIndexIsPointed = false;
        [SerializeField] float indexPointedThreshold = 20f;

        protected virtual void Awake()
        {
            hardwareHand = GetComponentInParent<HardwareHand>();
            hardwareRig = GetComponentInParent<HardwareRig>();
            if (hardwareHand == null) Debug.LogError("Missing hardware hand");
            if (hardwareRig == null) Debug.LogError("Missing hardware rig");
            if (hardwareHand)
            {
                xrHandsCollectableSkeletonDriver = hardwareHand.GetComponentInChildren<XRHandCollectableSkeletonDriver>();
            }
        }

        protected virtual void Start()
        {
            XRHandSubsystem m_Subsystem =
                XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();

            if(m_Subsystem != null) m_Subsystem.updatedHands += UpdatedHands;
        }

        protected virtual void OnDestroy()
        {
            XRHandSubsystem m_Subsystem =
                XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();

            if (m_Subsystem != null) m_Subsystem.updatedHands -= UpdatedHands;
        }

        protected virtual void OnDisable()
        {
            DesactivateInputControl();
        }

        protected virtual void DesactivateInputControl()
        {
            if (areInputControlled == false) return;
            areInputControlled = false;
            OnDesactivateInputcontrol();
        }

        protected virtual void ActivateInputControl()
        {
            if (areInputControlled) return;
            areInputControlled = true;
            OnActivateInputControl();
        }

        protected abstract void OnActivateInputControl();
        protected abstract void OnDesactivateInputcontrol();
        protected abstract void UpdatedHands(XRHand hand);

        #region XRHandSubsystem
        protected virtual void UpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags succesFlags, XRHandSubsystem.UpdateType updateType)
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
                        lastXRHand = hand;
                        ActivateInputControl();
                        UpdatedHands(hand);
                    }
                    else
                    {
                        DesactivateInputControl();
                    }
                    break;
            }
        }
        #endregion

        #region Pose access 
        public bool TryGetXRHandBonePose(XRHand hand, XRHandJointID jointId, out Pose rigRelativePose, out Pose worldPose)
        {
            worldPose = default;
            var joint = hand.GetJoint(jointId);
            var available = joint.TryGetPose(out rigRelativePose);
            if (available)
            {
                worldPose.position = hardwareRig.transform.TransformPoint(rigRelativePose.position);
                worldPose.rotation = hardwareRig.transform.rotation * rigRelativePose.rotation;
                return true;
            }
            return false;
        }

        public bool TryGetBonePose(XRHand hand, XRHandJointID jointId, out Pose rigRelativePose, out Pose worldPose)
        {
            if (xrHandsCollectableSkeletonDriver != null && xrHandsCollectableSkeletonDriver.TryJointWorldPose(jointId, out worldPose))
            {
                rigRelativePose.position = hardwareRig.transform.InverseTransformPoint(worldPose.position);
                rigRelativePose.rotation = Quaternion.Inverse(hardwareRig.transform.rotation) * worldPose.rotation;
                return true;
            }
            else if (TryGetXRHandBonePose(hand, jointId, out rigRelativePose, out worldPose))
            {
                // Fallback if no hand collector is available
                return true;
            }
            return false;
        }

        static public float NormalisedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
        #endregion

        #region Grabbing analysis
        static public bool GrabbingFinger(Pose metacarpalPose, Pose proximalPose, Pose intermediatePose, float grabbingProximalMinXRot, float grabbingIntermediateMinXRot)
        {
            return GrabbingFinger(metacarpalPose, proximalPose, intermediatePose, out _, out _, grabbingProximalMinXRot, grabbingIntermediateMinXRot);
        }

        static public bool GrabbingFinger(Pose metacarpalPose, Pose proximalPose, Pose intermediatePose, out float proximalX, out float intermediateX, float grabbingProximalMinXRot, float grabbingIntermediateMinXRot)
        {

            var localProximalRotation = Quaternion.Inverse(metacarpalPose.rotation) * proximalPose.rotation;
            var localIntermadiateRotation = Quaternion.Inverse(proximalPose.rotation) * intermediatePose.rotation;
            proximalX = NormalisedAngle(localProximalRotation.eulerAngles.x);
            intermediateX = NormalisedAngle(localIntermadiateRotation.eulerAngles.x);

            return proximalX > grabbingProximalMinXRot && intermediateX > grabbingIntermediateMinXRot;
        }

        public bool IsGrabbing(XRHand hand, float grabbingProximalMinXRot, float grabbingIntermediateMinXRot)
        {
            var middleProximalAvailable = TryGetBonePose(hand, XRHandJointID.MiddleProximal, out var middleProximalPose, out _);
            var ringProximalAvailable = TryGetBonePose(hand, XRHandJointID.RingProximal, out var ringProximalPose, out _);
            var middleIntermediateAvailable = TryGetBonePose(hand, XRHandJointID.MiddleIntermediate, out var middleIntermediatePose, out _);
            var ringIntermediateAvailable = TryGetBonePose(hand, XRHandJointID.RingIntermediate, out var ringIntermediatePose, out _);
            var ringMetacarpalAvailable = TryGetBonePose(hand, XRHandJointID.RingMetacarpal, out var ringMetacarpalPose, out _);
            var middleMetacarpalAvailable = TryGetBonePose(hand, XRHandJointID.MiddleMetacarpal, out var middleMetacarpalPose, out _);


            // disable the grab if the index is pointing
            if (disableGrabIfIndexIsPointed)
            {
                var indexProximalAvailable = TryGetBonePose(hand, XRHandJointID.IndexProximal, out var indexProximalPose, out _);
                var indexIntermediateAvailable = TryGetBonePose(hand, XRHandJointID.IndexIntermediate, out var indexIntermediatePose, out _);
                if (indexProximalAvailable && indexIntermediateAvailable)
                {
                    var indexIX = NormalisedAngle((Quaternion.Inverse(indexProximalPose.rotation) * indexIntermediatePose.rotation).eulerAngles.x);

                    if (indexIX < indexPointedThreshold)
                    {
                        return false;
                    }
                }
            }

            // Grab
            if (middleMetacarpalAvailable && middleIntermediateAvailable && middleProximalAvailable && ringMetacarpalAvailable && ringProximalAvailable && ringIntermediateAvailable)
            {

                if (GrabbingFinger(middleMetacarpalPose, middleProximalPose, middleIntermediatePose, grabbingProximalMinXRot, grabbingIntermediateMinXRot)
                    && GrabbingFinger(ringMetacarpalPose, ringProximalPose, ringIntermediatePose, grabbingProximalMinXRot, grabbingIntermediateMinXRot))
                {
                    return true;
                }
            }
            else
            {
                Debug.LogError($"Missing bone for grab check");
            }
            return false;
        }
        #endregion 
    }

}
