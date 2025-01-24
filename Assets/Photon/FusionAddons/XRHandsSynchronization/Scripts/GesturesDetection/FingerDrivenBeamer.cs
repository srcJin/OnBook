using Fusion.XR.Shared.Locomotion;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Fusion.Addons.XRHandsSync
{
    public class FingerDrivenBeamer : FingerDrivenGesture
    {
        RayBeamer beamer;
        Pose rayDefaultPose;
        bool beamerDefaultUseRayActionInput = false;

        [SerializeField]
        float maxThumbIndexAngleForBeam = 20;
        [SerializeField]
        float poseDurationBeforeBeamActivation = 0.5f;
        float wasActiveStart = -1;
        bool wasActive = false;

        float beamStartProximalMinXRot = 40f;
        float beamStartIntermediateMinXRot = 40f;

        #region FingerDrivenGesture
        protected override void OnActivateInputControl()
        {
            beamer.useRayActionInput = false;
        }

        protected override void OnDesactivateInputcontrol()
        {
            if (wasActive)
            {
                beamer.CancelHit();
            }
            beamer.useRayActionInput = beamerDefaultUseRayActionInput;
            beamer.isRayEnabled = false;
            // Pose was captured relatively to the hand
            beamer.origin.position = hardwareHand.transform.TransformPoint(rayDefaultPose.position);
            beamer.origin.rotation = hardwareHand.transform.rotation * rayDefaultPose.rotation;
            wasActiveStart = -1;
        }

        protected override void UpdatedHands(XRHand hand)
        {
            bool actionDetected = false;
            Pose beamerWorldPose = rayDefaultPose;
            bool poseAnalysed = false;


            var indexTipAvailable = TryGetBonePose(hand, XRHandJointID.IndexTip, out var indexTipPose, out _);
            var indexMetacarpalAvailable = TryGetBonePose(hand, XRHandJointID.IndexMetacarpal, out var indexMetacarpalPose, out var indexMetacarpalWorldPose);
            var indexProximalAvailable = TryGetBonePose(hand, XRHandJointID.IndexProximal, out var indexProximalPose, out _);
            var indexIntermediateAvailable = TryGetBonePose(hand, XRHandJointID.IndexIntermediate, out var indexIntermediatePose, out _);
            var indexDistalAvailable = TryGetBonePose(hand, XRHandJointID.IndexDistal, out var indexDistalPose, out _);

            var thumbMetacarpalAvailable = TryGetBonePose(hand, XRHandJointID.ThumbMetacarpal, out var thumbMetacarpalPose, out _);
            var thumbProximalAvailable = TryGetBonePose(hand, XRHandJointID.ThumbProximal, out var thumbProximalPose, out _);
            var thumbDistalAvailable = TryGetBonePose(hand, XRHandJointID.ThumbDistal, out var thumbDistalPose, out _);

            // Index
            if (indexTipAvailable && indexDistalAvailable && indexIntermediateAvailable && indexProximalAvailable && thumbDistalAvailable && thumbProximalAvailable && indexMetacarpalAvailable && thumbMetacarpalAvailable)
            {
                poseAnalysed = true;
                bool indexPoseOk = false;
                bool thumbPoseOk = false;
                var indexPX = NormalisedAngle((Quaternion.Inverse(indexMetacarpalPose.rotation) * indexProximalPose.rotation).eulerAngles.x);
                var indexIX = NormalisedAngle((Quaternion.Inverse(indexProximalPose.rotation) * indexIntermediatePose.rotation).eulerAngles.x);
                var indexDX = NormalisedAngle((Quaternion.Inverse(indexIntermediatePose.rotation) * indexDistalPose.rotation).eulerAngles.x);

                var thumbPX = NormalisedAngle((Quaternion.Inverse(thumbMetacarpalPose.rotation) * thumbProximalPose.rotation).eulerAngles.x);
                var thumbDX = NormalisedAngle((Quaternion.Inverse(thumbProximalPose.rotation) * thumbDistalPose.rotation).eulerAngles.x);

                if (indexPX < maxThumbIndexAngleForBeam && indexIX < maxThumbIndexAngleForBeam && indexDX < maxThumbIndexAngleForBeam)
                {
                    indexPoseOk = true;
                }
                if (
                    thumbPX < maxThumbIndexAngleForBeam
                    && thumbDX < maxThumbIndexAngleForBeam
                    )
                {
                    thumbPoseOk = true;
                }

                if (thumbPoseOk && (indexPoseOk || wasActive))
                {

                    actionDetected = true;
                    beamerWorldPose = indexMetacarpalWorldPose;
                }
            } else
            {
                Debug.LogError("Missing bones for beamer");
            }

            if (poseAnalysed == false && wasActive)
            {
                beamer.CancelHit();
            }

            if (actionDetected && wasActiveStart == -1)
            {
                // To start the pose, we check that the other parts of the hand are closed (like grabbing). Note that pinky is ignored

                if (IsGrabbing(hand, beamStartProximalMinXRot, beamStartIntermediateMinXRot) == false)
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
                if (wasActive == false) Debug.LogError("Starting beam");
                beamer.isRayEnabled = true;
                beamer.origin.position = beamerWorldPose.position;
                beamer.origin.rotation = beamerWorldPose.rotation;
            }
            else
            {
                if (wasActive) Debug.LogError("Sopping beam");
                beamer.isRayEnabled = false;
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
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            beamer = GetComponentInParent<RayBeamer>();
            if (beamer == null)
                Debug.LogError("Missing beamer");
            beamerDefaultUseRayActionInput = beamer.useRayActionInput;
            rayDefaultPose = new Pose(
                hardwareHand.transform.InverseTransformPoint(beamer.origin.position),
                Quaternion.Inverse(hardwareHand.transform.rotation) * beamer.origin.rotation
            );
        }
    }

}
