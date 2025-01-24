using Fusion.Addons.HandsSync;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.XRHandsSync
{
    [CreateAssetMenu(fileName = "BonesState", menuName = "Fusion Addons/BonesState", order = 1)]
    public class BonesStateScriptableObject : ScriptableObject
    {

        public Dictionary<HandSynchronizationBoneId, Vector3> bonePositionsByBoneId = new Dictionary<HandSynchronizationBoneId, Vector3>();
        public List<BonePosition> bonePositions = new List<BonePosition>();

        private void OnEnable()
        {
            bonePositionsByBoneId.Clear();
            foreach(var info in bonePositions)
            {
                bonePositionsByBoneId[info.boneId] = info.position;
            }
        }
    }

}
