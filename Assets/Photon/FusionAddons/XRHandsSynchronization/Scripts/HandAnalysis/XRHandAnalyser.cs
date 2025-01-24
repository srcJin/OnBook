using Fusion.Addons.HandsSync;
using Fusion.Addons.XRHandsSync;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Fusion.Addons.XRHandsSync
{
    [System.Serializable]

    public class XRhandAnalyser
    {
        public bool analyseHand = false;
        public bool analyseAverageBonesEulerRotation = false;
        public BonePosition[] initialPoses = null;
        bool initialPosesInitialized = false;
        public List<BonePose> debugPoseInfos = new List<BonePose>();
        public JoinInfo[] debugJointInfo = null;
        public Dictionary<HandSynchronizationBoneId, HandBoneInfo> recommandedCompressionBonesInfoByBoneId = new Dictionary<HandSynchronizationBoneId, HandBoneInfo>();
        public List<HandBoneInfo> recommandedCompressionBonesInfo = new List<HandBoneInfo>();


        [System.Serializable]
        public struct JoinInfo
        {
            public HandSynchronizationBoneId boneId;
            public float maxRotX;
            public float minRotX;
            public float maxRotY;
            public float minRotY;
            public float maxRotZ;
            public float minRotZ;
            public int deltaX;
            public int deltaY;
            public int deltaZ;
            public Vector3[] lastRotations;
            public Vector3 averageRotation;
            public int lastRecordedRotationIndex;
            public int lastRotationsCount;
            public const int MAX_LAST_ROTATIONS = 20;

            HandBoneInfo _recommandedBoneInfo;
            public HandBoneInfo RecommandedBoneInfo
            {
                get
                {
                    UpdateRecommandedBoneInfo();
                    return _recommandedBoneInfo;
                }
            }

            const float MIN_VARIABLE_AXIS_RANGE = 10f;
            void UpdateRecommandedBoneInfo()
            {
                _recommandedBoneInfo.boneId = boneId;
                _recommandedBoneInfo.XminRange = minRotX;
                _recommandedBoneInfo.XmaxRange = maxRotX;
                _recommandedBoneInfo.YminRange = minRotY;
                _recommandedBoneInfo.YmaxRange = maxRotY;
                _recommandedBoneInfo.ZminRange = minRotZ;
                _recommandedBoneInfo.ZmaxRange = maxRotZ;
                _recommandedBoneInfo.applyOffset = false;
                _recommandedBoneInfo.offsetValue = Vector3.zero;
                bool deltaX = Mathf.Abs(maxRotX - minRotX) > MIN_VARIABLE_AXIS_RANGE;
                bool deltaY = Mathf.Abs(maxRotY - minRotY) > MIN_VARIABLE_AXIS_RANGE;
                bool deltaZ = Mathf.Abs(maxRotZ - minRotZ) > MIN_VARIABLE_AXIS_RANGE;
                if (deltaX && deltaY && deltaZ)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.XYZ;
                }
                else if (deltaX && deltaY)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.XY;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(0, 0, averageRotation.z);
                }
                else if (deltaX && deltaZ)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.XZ;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(0, averageRotation.y, 0);

                }
                else if (deltaY && deltaZ)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.YZ;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(averageRotation.x, 0, 0);
                }
                else if (deltaX)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.X;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(0, averageRotation.y, averageRotation.z);
                }
                else if (deltaY)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.Y;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(averageRotation.x, 0, averageRotation.z);
                }
                else if (deltaZ)
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.Z;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(averageRotation.x, averageRotation.y, 0);
                }
                else
                {
                    _recommandedBoneInfo.axisCompressionMode = BoneAxisCompressionMode.HardcodedValue;
                    _recommandedBoneInfo.applyOffset = true;
                    _recommandedBoneInfo.offsetValue = new Vector3(averageRotation.x, averageRotation.y, averageRotation.z);
                }
            }
        }

        public void StartBonesAnalysis(int maxBoneCount)
        {
            debugPoseInfos.Clear();
            if (analyseHand && (initialPoses == null || initialPoses.Length != maxBoneCount))
            {
                initialPoses = new BonePosition[maxBoneCount];
            }
            if (analyseHand && (debugJointInfo == null || debugJointInfo.Length != maxBoneCount))
            {
                debugJointInfo = new JoinInfo[maxBoneCount];
            }
        }

        public void AddPose(int index, Pose pose, HandSynchronizationBoneId boneId)
        {
            if (analyseHand && initialPosesInitialized == false)
            {
                initialPoses[index].boneId = boneId;
                initialPoses[index].position = pose.position;
            }

            if (analyseHand)
            {
                debugPoseInfos.Add(new BonePose { boneId = boneId, pose = pose });

                if (analyseAverageBonesEulerRotation)
                {
                    var euler = pose.rotation.eulerAngles;
                    euler.x = CompressedHandState.NormalizedAngle(euler.x);
                    euler.y = CompressedHandState.NormalizedAngle(euler.y);
                    euler.z = CompressedHandState.NormalizedAngle(euler.z);
                    if (debugJointInfo[index].boneId != boneId)
                    {
                        debugJointInfo[index].boneId = boneId;
                        debugJointInfo[index].maxRotX = euler.x; debugJointInfo[index].minRotX = euler.x;
                        debugJointInfo[index].maxRotY = euler.y; debugJointInfo[index].minRotY = euler.y;
                        debugJointInfo[index].maxRotZ = euler.z; debugJointInfo[index].minRotZ = euler.z;
                    }
                    else
                    {
                        if (debugJointInfo[index].maxRotX < euler.x) debugJointInfo[index].maxRotX = euler.x;
                        if (debugJointInfo[index].minRotX > euler.x) debugJointInfo[index].minRotX = euler.x;
                        if (debugJointInfo[index].maxRotY < euler.y) debugJointInfo[index].maxRotY = euler.y;
                        if (debugJointInfo[index].minRotY > euler.y) debugJointInfo[index].minRotY = euler.y;
                        if (debugJointInfo[index].maxRotZ < euler.z) debugJointInfo[index].maxRotZ = euler.z;
                        if (debugJointInfo[index].minRotZ > euler.z) debugJointInfo[index].minRotZ = euler.z;
                    }
                    if (debugJointInfo[index].lastRotations == null || debugJointInfo[index].lastRotations.Length != JoinInfo.MAX_LAST_ROTATIONS)
                    {
                        debugJointInfo[index].lastRotations = new Vector3[JoinInfo.MAX_LAST_ROTATIONS];
                        debugJointInfo[index].lastRecordedRotationIndex = 0;
                        debugJointInfo[index].lastRotationsCount = 0;
                    }
                    debugJointInfo[index].lastRotations[debugJointInfo[index].lastRecordedRotationIndex] = euler;
                    debugJointInfo[index].lastRecordedRotationIndex = (debugJointInfo[index].lastRecordedRotationIndex + 1) % JoinInfo.MAX_LAST_ROTATIONS;
                    if (debugJointInfo[index].lastRotationsCount != JoinInfo.MAX_LAST_ROTATIONS) debugJointInfo[index].lastRotationsCount++;
                    debugJointInfo[index].averageRotation = Vector3.zero;
                    for (int j = 0; j < debugJointInfo[index].lastRotationsCount; j++)
                    {
                        debugJointInfo[index].averageRotation += debugJointInfo[index].lastRotations[j];
                    }
                    debugJointInfo[index].averageRotation /= debugJointInfo[index].lastRotationsCount;
                    debugJointInfo[index].deltaX = (int)(debugJointInfo[index].maxRotX - debugJointInfo[index].minRotX);
                    debugJointInfo[index].deltaY = (int)(debugJointInfo[index].maxRotY - debugJointInfo[index].minRotY);
                    debugJointInfo[index].deltaZ = (int)(debugJointInfo[index].maxRotZ - debugJointInfo[index].minRotZ);

                    recommandedCompressionBonesInfoByBoneId[boneId] = debugJointInfo[index].RecommandedBoneInfo;
                }
            }
        }

        public void EndBonesAnalysis()
        {
            initialPosesInitialized = true;
            if (analyseAverageBonesEulerRotation)
            {
                recommandedCompressionBonesInfo.Clear();
                foreach (var entry in recommandedCompressionBonesInfoByBoneId) recommandedCompressionBonesInfo.Add(entry.Value);
            }
        }
    }
}