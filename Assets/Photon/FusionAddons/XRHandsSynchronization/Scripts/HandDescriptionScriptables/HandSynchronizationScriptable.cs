using UnityEngine;
using System.Collections.Generic;

namespace Fusion.Addons.HandsSync
{
    [CreateAssetMenu(fileName = "HandSynchronization", menuName = "Fusion Addons/HandSynchronizationScriptable", order = 1)]

    public class HandSynchronizationScriptable : ScriptableObject
    {
        public List<HandBoneInfo> bonesInfo = new List<HandBoneInfo>
        {
        };

        [Header("Emission limitation")]
        public bool limitHandChangeEmissionPersecond = false;
        public float handChangeEmissionMinDelay = 0.1f;

        [Header("Rendering smoothing")]
        [Tooltip("Interpolate the rotation of the bones, interpolationDelay seconds in the past")]
        public bool interpolateBoneRotations = true;
        public int storedInterpolationStates = 10;
        public float interpolationDelay = 0.1f;

        [Header("Performance settings")]
        public bool uncompressingBoneDataOnNetworkChangesOnly = true;

        [Header("Debug")]
        public bool debugRemoteRenderingLocally = false;

        // Cached version for quick access
        public Dictionary<HandSynchronizationBoneId, HandBoneInfo> boneInfoByBoneId = new Dictionary<HandSynchronizationBoneId, HandBoneInfo>();

        private void OnEnable()
        {
            ResetBoneInfo();
            UpdateBonesInfo();
        }

        private int byteSize = 0;
        private bool boneInfoComputed = false;
        public int BoneInfoByteSize
        {
            get
            {
                if (boneInfoComputed == false)
                {
                    boneInfoComputed = true;
                    UpdateBonesInfo();
                }
                return byteSize;
            }
        }

        public void ResetBoneInfo()
        {
            boneInfoComputed = false;
        }

        public void UpdateBonesInfo()
        {
            boneInfoComputed = true;
            boneInfoByBoneId = new Dictionary<HandSynchronizationBoneId, HandBoneInfo>();
            byteSize = 0;
            foreach (var boneInfo in bonesInfo)
            {
                if (boneInfoByBoneId.ContainsKey(boneInfo.boneId)) Debug.LogError("Duplicate bone id in HandSynchronizationScriptable " + name);
                boneInfoByBoneId[boneInfo.boneId] = boneInfo;
                byteSize += boneInfo.ByteSize;
            }
        }

        [ContextMenu("Log total byte size")]
        public void LogTotalSize()
        {
            ResetBoneInfo();
            Debug.Log("Total byte size: " + BoneInfoByteSize);
        }
    }
}
