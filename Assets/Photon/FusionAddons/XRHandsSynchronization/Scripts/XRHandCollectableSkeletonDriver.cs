using Fusion.Addons.HandsSync;
using Fusion.Addons.XRHandsSync;
using Fusion.XR.Shared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

namespace Fusion.Addons.XRHandsSync
{
    public class XRHandCollectableSkeletonDriver : XRHandSkeletonDriver, IBonesCollecter
    {
        public enum ControllerTrackingMode
        {
            AlwaysAvailable,
            NeverAvailable,
            UseInputAction
        }
        [Header("Controller tracking fallback")]
        public ControllerTrackingMode controllerTrackingMode = XRHandCollectableSkeletonDriver.ControllerTrackingMode.UseInputAction;
        [DrawIf(nameof(controllerTrackingMode), (long)XRHandCollectableSkeletonDriver.ControllerTrackingMode.UseInputAction, CompareOperator.Equal, Hide = true)]
        public InputActionProperty controllerAvailableAction = new InputActionProperty();

        [Header("Hardware hand relativepositioning")]
        public Transform handRoot;
        [Tooltip("Apply wrist pose to a parent object instead of the wrist bone")]
        public bool applyWristPoseToHandRoot = true;
        [Tooltip("Force IBonesCollecter.CurrentBonesPoses to return Pose.identity for the wrist (useful if we consider the wrist to be at the root of the hand)")]
        public bool forceWristPoseToIdentity = true;

        [SerializeField] XR.Shared.Rig.HardwareHand hardwareHand;

        [Header("Collected data")]

        Dictionary<HandSynchronizationBoneId, Pose> _currentPoses = new Dictionary<HandSynchronizationBoneId, Pose>();

        public Dictionary<HandSynchronizationBoneId, Pose> CurrentBonesPoses => _currentPoses;

        Dictionary<HandSynchronizationBoneId, Quaternion> _currentBoneRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();

        public Dictionary<HandSynchronizationBoneId, Quaternion> CurrentBoneRotations => _currentBoneRotations;

        [Header("Tracking state")]
        [SerializeField]
        bool isFingerTrackingAvailable;
        [SerializeField]
        bool isControllerTrackingAvailable;

        public HandTrackingMode CurrentHandTrackingMode
        { 
            get
            {
                isFingerTrackingAvailable = handTrackingEvents == null || handTrackingEvents.handIsTracked;
                if (controllerTrackingMode == ControllerTrackingMode.AlwaysAvailable)
                {
                    isControllerTrackingAvailable = true;
                }
                else if (controllerTrackingMode == ControllerTrackingMode.UseInputAction && controllerAvailableAction.action != null && controllerAvailableAction.action.ReadValue<float>() == 1)
                {
                    isControllerTrackingAvailable = true;
                } 
                else
                {
                    isControllerTrackingAvailable = false;
                }
                if (isFingerTrackingAvailable)
                {
                    return HandTrackingMode.FingerTracking;
                }
                else if (isControllerTrackingAvailable)
                {
                    return HandTrackingMode.ControllerTracking;
                }
                return HandTrackingMode.NotTracked;
            }
        }

        private void Awake()
        {
            if(hardwareHand == null) hardwareHand = GetComponentInParent<XR.Shared.Rig.HardwareHand>();
            if(hardwareHand == null) 
                Debug.LogError("Missing hardware hand parent");
            if (controllerTrackingMode == ControllerTrackingMode.UseInputAction)
            {
                if (hardwareHand)
                {
                    controllerAvailableAction.EnableWithDefaultXRBindings(side: hardwareHand.side, new List<string> { "isTracked" });
                }
            }
            if (applyWristPoseToHandRoot && handRoot == null)
            {
                if(hardwareHand)
                {
                    handRoot = hardwareHand.transform;
                }
                else
                {
                    Debug.LogError("Hand root not defined");
                }

            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (handTrackingEvents == null)
            {
                Debug.LogError("Missing XRHandTrackingEvents: IsFingerTrackingAvailable will always return true");
            }
        }

#if UNITY_EDITOR
        [Header("Debug - Hand analysis ")]
        public XRhandAnalyser analyser = new XRhandAnalyser();

        [Header("Debug - Mirror hand")]
        public bool sendPositionsToDebugMirrorSkeleton = false;
        public XRHandRemoteSkeletonDriver debugMirrorSkeleton;

#endif

        public bool TryJointWorldPose(XRHandJointID jointId, out Pose worldPose)
        {
            worldPose = default;
            var jointIndex = jointId.ToIndex();
            if (jointIndex >= m_JointTransforms.Length) return false;
            worldPose.position = m_JointTransforms[jointIndex].position;
            worldPose.rotation = m_JointTransforms[jointIndex].rotation;
            return true;
        }

        protected override void OnRootPoseUpdated(Pose rootPose)
        {
            if (applyWristPoseToHandRoot && handRoot != null)
            {
                var wristIndex = XRHandJointID.Wrist.ToIndex();
                handRoot.localPosition = rootPose.position;
                handRoot.localRotation = rootPose.rotation;
                m_JointLocalPoses[wristIndex] = Pose.identity;
            }
        }

        protected override void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            UpdateJointLocalPoses(args);
            if (applyWristPoseToHandRoot)
            {
                var wristIndex = XRHandJointID.Wrist.ToIndex();
                m_JointLocalPoses[wristIndex] = Pose.identity;
            }
            ApplyUpdatedTransformPoses();

            _currentPoses.Clear();


#if UNITY_EDITOR
            analyser.StartBonesAnalysis(m_JointLocalPoses.Length);
#endif
            for (int i = 0; i < m_JointLocalPoses.Length; i++)
            {
                var jointId = XRHandJointIDUtility.FromIndex(i);
                var boneId = jointId.AsHandSynchronizationBoneId();
                var pose = m_JointLocalPoses[i];
                if (forceWristPoseToIdentity && boneId == HandSynchronizationBoneId.Hand_WristRoot)
                {
                    _currentPoses[boneId] = Pose.identity;
                }
                else
                {
                    _currentPoses[boneId] = pose;
                }
#if UNITY_EDITOR
                analyser.AddPose(index: i, pose, boneId);
#endif
            }

            _currentBoneRotations.Clear();
            foreach (var boneId in _currentPoses.Keys)
            {
                _currentBoneRotations[boneId] = _currentPoses[boneId].rotation;

            }

#if UNITY_EDITOR
            analyser.EndBonesAnalysis();

            if (debugMirrorSkeleton)
            {
                Dictionary<HandSynchronizationBoneId, Pose> appliedPoses = new Dictionary<HandSynchronizationBoneId, Pose>();

                foreach (var boneId in _currentPoses.Keys)
                {
                    if (sendPositionsToDebugMirrorSkeleton)
                    {
                        appliedPoses[boneId] = new Pose { position = _currentPoses[boneId].position, rotation = _currentPoses[boneId].rotation };
                    }
                    else
                    {
                        appliedPoses[boneId] = new Pose { position = Vector3.zero, rotation = _currentPoses[boneId].rotation };
                    }
                }
                debugMirrorSkeleton.ApplyPoses(appliedPoses);
            }
#endif
        }

#if BURST_PRESENT
        [BurstCompile]
#endif
        static void CalculateLocalTransformPose(in Pose parentPose, in Pose jointPose, out Pose jointLocalPose)
        {
            var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
            jointLocalPose.position = inverseParentRotation * (jointPose.position - parentPose.position);
            jointLocalPose.rotation = inverseParentRotation * jointPose.rotation;
        }
    }

    public static class XRHandsSyncExtension
    {
        public static HandSynchronizationBoneId AsHandSynchronizationBoneId(this XRHandJointID jointId)
        {
            HandSynchronizationBoneId boneId = HandSynchronizationBoneId.Invalid;
            switch (jointId)
            {
                case XRHandJointID.Wrist:
                    boneId = HandSynchronizationBoneId.Hand_WristRoot;
                    break;
                case XRHandJointID.Palm:
                    boneId = HandSynchronizationBoneId.Hand_Palm;
                    break;
                case XRHandJointID.ThumbMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb0;
                    break;
                case XRHandJointID.ThumbProximal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb1;
                    break;
                case XRHandJointID.ThumbDistal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb2;
                    break;
                case XRHandJointID.ThumbTip:
                    boneId = HandSynchronizationBoneId.Hand_ThumbTip;
                    break;
                case XRHandJointID.IndexMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Index0;
                    break;
                case XRHandJointID.IndexProximal:
                    boneId = HandSynchronizationBoneId.Hand_Index1;
                    break;
                case XRHandJointID.IndexIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Index2;
                    break;
                case XRHandJointID.IndexDistal:
                    boneId = HandSynchronizationBoneId.Hand_Index3;
                    break;
                case XRHandJointID.IndexTip:
                    boneId = HandSynchronizationBoneId.Hand_IndexTip;
                    break;
                case XRHandJointID.MiddleMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Middle0;
                    break;
                case XRHandJointID.MiddleProximal:
                    boneId = HandSynchronizationBoneId.Hand_Middle1;
                    break;
                case XRHandJointID.MiddleIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Middle2;
                    break;
                case XRHandJointID.MiddleDistal:
                    boneId = HandSynchronizationBoneId.Hand_Middle3;
                    break;
                case XRHandJointID.MiddleTip:
                    boneId = HandSynchronizationBoneId.Hand_MiddleTip;
                    break;
                case XRHandJointID.RingMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Ring0;
                    break;
                case XRHandJointID.RingProximal:
                    boneId = HandSynchronizationBoneId.Hand_Ring1;
                    break;
                case XRHandJointID.RingIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Ring2;
                    break;
                case XRHandJointID.RingDistal:
                    boneId = HandSynchronizationBoneId.Hand_Ring3;
                    break;
                case XRHandJointID.RingTip:
                    boneId = HandSynchronizationBoneId.Hand_RingTip;
                    break;
                case XRHandJointID.LittleMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky0;
                    break;
                case XRHandJointID.LittleProximal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky1;
                    break;
                case XRHandJointID.LittleIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Pinky2;
                    break;
                case XRHandJointID.LittleDistal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky3;
                    break;
                case XRHandJointID.LittleTip:
                    boneId = HandSynchronizationBoneId.Hand_PinkyTip;
                    break;
            }
            return boneId;
        }
    }

}
