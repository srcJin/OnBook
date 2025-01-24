using Fusion.Addons.HandsSync;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Fusion.Addons.XRHandsSync
{
    public class XRHandRemoteSkeletonDriver : XRHandSkeletonDriver, IBonesReader
    {
        // Start is called before the first frame update
        protected override void OnEnable()
        {
            m_JointLocalPoses = new NativeArray<Pose>(XRHandJointID.EndMarker.ToIndex(), Unity.Collections.Allocator.Persistent);

            foreach (var joint in m_JointTransformReferences)
            {
                var jointIndex = joint.xrHandJointID.ToIndex();
                if (jointIndex < 0 || jointIndex >= m_JointTransforms.Length)
                {
                    Debug.LogWarning($"{nameof(XRHandSkeletonDriver)} has an invalid joint reference set: {joint.xrHandJointID}", this);
                }
            }
        }

        public void ApplyPoses(Dictionary<HandSynchronizationBoneId, Pose> posesByboneId)
        {
            int posesCount = m_JointLocalPoses.Length;
            if (debugAppliedPoses && (appliedPoses == null || appliedPoses.Length != posesCount)) appliedPoses = new Pose[posesCount];
            for (int i = 0; i < posesCount; i++)
            {
                var jointId = XRHandJointIDUtility.FromIndex(i);
                var boneId = jointId.AsHandSynchronizationBoneId();
                var pose = Pose.identity;
                if (posesByboneId.ContainsKey(boneId))
                {
                    pose = posesByboneId[boneId];
                }
                else
                {
                    Debug.LogError("Missing bone: "+jointId);
                }

                m_JointLocalPoses[i] = pose;
                if(debugAppliedPoses) appliedPoses[i] = pose;
            }
            ApplyUpdatedTransformPoses();
        }

        [Header("Debug")]
        public bool debugAppliedPoses = false;
        public Pose[] appliedPoses;
    }
}
