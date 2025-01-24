#if OCULUS_SDK_AVAILABLE
using Fusion.Addons.HandsSync;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

namespace Fusion.Addons.HandsSync.Meta
{
    public class OVRHandsBonesCollecter : MonoBehaviour, IBonesCollecter
    {
        public OVRHand ovrHand;
        HardwareHand hardwareHand;

        [Header("Adapt hand location")]
        public bool adaptHandLocalisation = true;
        public Transform handTarget;
        public Vector3 handTrackingWristPositionOffset;
        public Vector3 handTrackingWristRotationOffset;
        public Vector3 controllerTrackingWristPositionOffset;
        public Vector3 controllerTrackingWristRotationOffset;
        OVRSkeleton _skeleton;

        [Header("Apply bone position")]
        public List<GameObject> controllerTrackingGameObjects = new List<GameObject>();
        public List<GameObject> fingerTrackingGameObjects = new List<GameObject>();
        public List<Transform> indexFollowers = new List<Transform>();
        public GameObject controllerTrackingIndexGameObject;
        #region OVR info
        public OVRSkeleton.SkeletonType OVRSkeletonType => ((OVRSkeleton.IOVRSkeletonDataProvider)ovrHand).GetSkeletonType();
        public OVRHand.Hand OVRHandType => OVRSkeletonType == OVRSkeleton.SkeletonType.HandLeft ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight; 
        public bool IsControllerInHand => OVRPlugin.GetControllerIsInHand(OVRPlugin.Step.Render, OVRHandType == OVRHand.Hand.HandLeft ? OVRPlugin.Node.ControllerLeft : OVRPlugin.Node.ControllerRight);
    #endregion

    #region IBonesCollecter
        public Dictionary<HandSynchronizationBoneId, Pose> CurrentBonesPoses => null;

        public Dictionary<HandSynchronizationBoneId, Quaternion> CurrentBoneRotations {
            get
            {
                Dictionary<HandSynchronizationBoneId, Quaternion> boneRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();
                Quaternion thumb0 = Quaternion.identity;
                Quaternion thumb1 = Quaternion.identity;

                if (SkeletonData.IsDataValid)
                {
                    int i = 0;
                    foreach (var r in SkeletonData.BoneRotations)
                    {
                        var boneId = (OVRSkeleton.BoneId)i;
                        var xrHandBoneId = boneId.AsHandSynchronizationBoneId();
                        if (boneId == OVRSkeleton.BoneId.Hand_Thumb0) thumb0 = r.ToXRHandsRot(OVRHandType, boneId);
                        if (boneId == OVRSkeleton.BoneId.Hand_Thumb1) thumb1 = r.ToXRHandsRot(OVRHandType, boneId);
                        if (xrHandBoneId != HandSynchronizationBoneId.Invalid)
                        {
                            var rot = r.ToXRHandsRot(OVRHandType, boneId);
                            boneRotations[xrHandBoneId] = rot;
                        }
                        i++;
                    }
                }
                var thumbRot = thumb0 * thumb1;
                boneRotations[HandSynchronizationBoneId.Hand_Thumb0] = thumbRot;
                boneRotations[HandSynchronizationBoneId.Hand_Index0] = Quaternion.identity;
                boneRotations[HandSynchronizationBoneId.Hand_Middle0] = Quaternion.identity;
                boneRotations[HandSynchronizationBoneId.Hand_Ring0] = Quaternion.identity;
                boneRotations[HandSynchronizationBoneId.Hand_Palm] = Quaternion.identity;
                return boneRotations;
            }
        }

        public HandTrackingMode CurrentHandTrackingMode {
            get
            {
                if (SkeletonData.IsDataValid) return HandTrackingMode.FingerTracking;
                if(IsControllerInHand) return HandTrackingMode.ControllerTracking;
                return HandTrackingMode.NotTracked;
            }
        }
    #endregion

        OVRSkeleton.SkeletonPoseData _skeletonData;
        bool skeletonDataCollectedForThisFrame = false;

        private void Awake()
        {
            if (handTarget == null) handTarget = transform;
            if (ovrHand == null) 
                Debug.LogError("OVR hand not set");
            else 
                _skeleton = ovrHand.GetComponent<OVRSkeleton>();
            hardwareHand = GetComponentInParent<HardwareHand>();
        }

        public OVRSkeleton.SkeletonPoseData SkeletonData
        {
            get
            {
                RefreshSkeletonData();
                return _skeletonData;
            }
        }
        void RefreshSkeletonData(bool forceRefresh = false)
        {
            if (skeletonDataCollectedForThisFrame == false || forceRefresh || ovrHand.IsDataValid != _skeletonData.IsDataValid)
            {
                var skeletonDataProvider = ((OVRSkeleton.IOVRSkeletonDataProvider)ovrHand);
                _skeletonData = skeletonDataProvider.GetSkeletonPoseData();
                skeletonDataCollectedForThisFrame = true;
            }
        }
        private void LateUpdate()
        {
            skeletonDataCollectedForThisFrame = false;
        }

        private void Update()
        {
            var trackingMode = CurrentHandTrackingMode;
            if (adaptHandLocalisation)
            {
                if (trackingMode == HandTrackingMode.FingerTracking)
                {
                    foreach (var bone in _skeleton.Bones)
                    {
                        if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
                        {
                            transform.position = bone.Transform.position + handTrackingWristPositionOffset;
                            transform.rotation = bone.Transform.rotation * Quaternion.Euler(handTrackingWristRotationOffset);
                            break;
                        }
                    }
                }
                else
                {
                    transform.localPosition = controllerTrackingWristPositionOffset;
                    transform.localRotation = Quaternion.identity * Quaternion.Euler(controllerTrackingWristRotationOffset);
                }
            }
            foreach (var go in controllerTrackingGameObjects)
            {
                go.SetActive(trackingMode == HandTrackingMode.ControllerTracking);
            }
            foreach (var go in fingerTrackingGameObjects)
            {
                go.SetActive(trackingMode == HandTrackingMode.FingerTracking);
            }

            // Index follower
            if (trackingMode == HandTrackingMode.FingerTracking)
            {
                foreach (var bone in _skeleton.Bones)
                {
                    if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                    {
                        foreach(var follower in indexFollowers)
                        {
                            follower.position = bone.Transform.position;
                            follower.rotation = bone.Transform.rotation;

                        }
                        break;
                    }
                }
            } 
            else if(controllerTrackingIndexGameObject)
            {
                foreach (var follower in indexFollowers)
                {
                    follower.position = controllerTrackingIndexGameObject.transform.position;
                    follower.rotation = controllerTrackingIndexGameObject.transform.rotation;

                }
            }

            // Finger tracking grabbing
            if (trackingMode == HandTrackingMode.FingerTracking)
            {
                hardwareHand.updateGrabWithAction = false;
                hardwareHand.isGrabbing = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            } 
            else
            {
                hardwareHand.updateGrabWithAction = true;
            }
        }
    }

    public static class QuatFConversion
    {
        public static Quaternion ToXRHandsRot(this OVRPlugin.Quatf q, OVRHand.Hand handType, OVRSkeleton.BoneId boneId)
        {
            bool isThumbBone = boneId == OVRSkeleton.BoneId.Hand_Thumb3 || boneId == OVRSkeleton.BoneId.Hand_Thumb2 || boneId == OVRSkeleton.BoneId.Hand_Thumb1 || boneId == OVRSkeleton.BoneId.Hand_Thumb0;

            if (isThumbBone)
            {
                if (handType == OVRHand.Hand.HandRight)
                {
                    return new Quaternion() { x = q.z, y = -q.y, z = q.x, w = q.w };
                }
                return new Quaternion() { x = q.z, y = q.y, z = -q.x, w = q.w };
            }
            return q.ToXRHandsRot(handType);
        } 
        
        public static Quaternion ToXRHandsRot(this OVRPlugin.Quatf q, OVRHand.Hand handType)
        {
            if (handType == OVRHand.Hand.HandRight)
            {
                return new Quaternion() { x = q.z, y = -q.y, z = -q.x, w = q.w };
            }
            return new Quaternion() { x = q.z, y = q.y, z = q.x, w = q.w };
        }
    }

    public static class HandSStateMetaExtensions
    {
        // AsHandSynchronizationBoneId returns the HandSynchronizationBoneId corresponding to the OVRSkeleton.BoneId parameter
        public static HandSynchronizationBoneId AsHandSynchronizationBoneId(this OVRSkeleton.BoneId source)
        {
            switch (source)
            {
                case OVRSkeleton.BoneId.Hand_WristRoot: return HandSynchronizationBoneId.Hand_WristRoot;
                case OVRSkeleton.BoneId.Hand_ForearmStub: return HandSynchronizationBoneId.Hand_ForearmStub;
                case OVRSkeleton.BoneId.Hand_Thumb0: return HandSynchronizationBoneId.Invalid;
                case OVRSkeleton.BoneId.Hand_Thumb1: return HandSynchronizationBoneId.Invalid;
                case OVRSkeleton.BoneId.Hand_Thumb2: return HandSynchronizationBoneId.Hand_Thumb1;
                case OVRSkeleton.BoneId.Hand_Thumb3: return HandSynchronizationBoneId.Hand_Thumb2;
                case OVRSkeleton.BoneId.Hand_Index1: return HandSynchronizationBoneId.Hand_Index1;
                case OVRSkeleton.BoneId.Hand_Index2: return HandSynchronizationBoneId.Hand_Index2;
                case OVRSkeleton.BoneId.Hand_Index3: return HandSynchronizationBoneId.Hand_Index3;
                case OVRSkeleton.BoneId.Hand_Middle1: return HandSynchronizationBoneId.Hand_Middle1;
                case OVRSkeleton.BoneId.Hand_Middle2: return HandSynchronizationBoneId.Hand_Middle2;
                case OVRSkeleton.BoneId.Hand_Middle3: return HandSynchronizationBoneId.Hand_Middle3;
                case OVRSkeleton.BoneId.Hand_Ring1: return HandSynchronizationBoneId.Hand_Ring1;
                case OVRSkeleton.BoneId.Hand_Ring2: return HandSynchronizationBoneId.Hand_Ring2;
                case OVRSkeleton.BoneId.Hand_Ring3: return HandSynchronizationBoneId.Hand_Ring3;
                case OVRSkeleton.BoneId.Hand_Pinky0: return HandSynchronizationBoneId.Hand_Pinky0;
                case OVRSkeleton.BoneId.Hand_Pinky1: return HandSynchronizationBoneId.Hand_Pinky1;
                case OVRSkeleton.BoneId.Hand_Pinky2: return HandSynchronizationBoneId.Hand_Pinky2;
                case OVRSkeleton.BoneId.Hand_Pinky3: return HandSynchronizationBoneId.Hand_Pinky3;
                case OVRSkeleton.BoneId.Hand_ThumbTip: return HandSynchronizationBoneId.Hand_ThumbTip;
                case OVRSkeleton.BoneId.Hand_IndexTip: return HandSynchronizationBoneId.Hand_IndexTip;
                case OVRSkeleton.BoneId.Hand_MiddleTip: return HandSynchronizationBoneId.Hand_MiddleTip;
                case OVRSkeleton.BoneId.Hand_RingTip: return HandSynchronizationBoneId.Hand_RingTip;
                case OVRSkeleton.BoneId.Hand_PinkyTip: return HandSynchronizationBoneId.Hand_PinkyTip;
            }
            return HandSynchronizationBoneId.Invalid;
        }
    }
}

#endif