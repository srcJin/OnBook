using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Rig;
using Fusion.Addons.XRHandsSync;

namespace Fusion.Addons.HandsSync
{
    public interface IBonesCollecter
    {
        Dictionary<HandSynchronizationBoneId, Pose> CurrentBonesPoses { get; }
        Dictionary<HandSynchronizationBoneId, Quaternion> CurrentBoneRotations { get; }
        HandTrackingMode CurrentHandTrackingMode { get; }
#pragma warning disable IDE1006 // Naming Styles
        GameObject gameObject { get; }
#pragma warning restore IDE1006 // Naming Styles
    }

    public interface IBonesReader
    {
        public void ApplyPoses(Dictionary<HandSynchronizationBoneId, Pose> posesByboneId);
    }

    [DefaultExecutionOrder(NetworkBonesStateSync.EXECUTION_ORDER)]
    public class NetworkBonesStateSync : NetworkBehaviour
    {
        public const int BONES_COUNT = 26;
        // First and last bones are ignored Bone 0 is wrist (trnasfered through NetworkHand) and bone 25 is palm, which does not rotate


        public BonesStateScriptableObject defaultBonePositions;

        [SerializeField] private HandSynchronizationScriptable handSynchronizationScriptable;
        List<HandBoneInfo> _compressionBonesInfo = null;
        List<HandBoneInfo> CompressionBonesInfo
        {
            get
            {
                if (handSynchronizationScriptable)
                {
                    return handSynchronizationScriptable.bonesInfo;
                }
                else
                {
                    if (_compressionBonesInfo == null || _compressionBonesInfo.Count == 0)
                    {
                        Debug.LogWarning("This component sync rotations, and a default positions set, provided through a BonesStateScriptableObject, is required. Using default uncompressed version.");
                        _compressionBonesInfo = new List<HandBoneInfo>();
                        foreach (var boneIdValue in System.Enum.GetValues(typeof(HandSynchronizationBoneId)))
                        {
                            var boneId = (HandSynchronizationBoneId)boneIdValue;
                            _compressionBonesInfo.Add(new HandBoneInfo { 
                                boneId = boneId,
                                axisCompressionMode = BoneAxisCompressionMode.XYZ,
                            });
                        }
                    }
                    return _compressionBonesInfo;
                }
            }
        }

        [Networked]
        public HandTrackingMode CurrentHandTrackingMode { get; set; }

        [Networked]
        public NetworkBool IsDataHighConfidence { get; set; }

        [Networked]
        public float HandScale { get; set; }

        const int BONE_DATA_SIZE = 19;          // the size should match handSynchronizationScriptable.BoneInfoByteSize, that you can see in the scriptable inspector in the total byte size field (only existing in the Editor)
        [SerializeField]
        [Networked, Capacity(BONE_DATA_SIZE)]
        NetworkArray<byte> CompressedBonesRotations { get; }

        CompressedHandState compressedHandState;
        HandState cacheHandState;

        #region Interpolation
        public bool useRotationsInterpolation = true;
        #endregion

        public const int EXECUTION_ORDER = NetworkHand.EXECUTION_ORDER + 10;
        public bool IsLocalPlayer => Object && Object.HasStateAuthority;
        IBonesCollecter localBoneCollecter;
        IBonesReader bonesReader;
        NetworkHand networkHand;

        [Header("Debug")]
        public bool debugDisplayNetworkStateForLocaluser = false;
        [DrawIf(nameof(debugDisplayNetworkStateForLocaluser), Hide = true)]
        public bool debugDisplayCompressionImprovements = false;
        Dictionary<HandSynchronizationBoneId, Quaternion> debugStoredRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();

        private void Awake()
        {
            networkHand = GetComponentInParent<NetworkHand>();
            bonesReader = GetComponentInChildren<IBonesReader>();
        }

        public override void Spawned()
        {
            base.Spawned();
            if (networkHand.IsLocalNetworkRig && networkHand.LocalHardwareHand)
            {
                localBoneCollecter = networkHand.LocalHardwareHand.GetComponentInChildren<IBonesCollecter>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (networkHand.IsLocalNetworkRig && localBoneCollecter != null)
            {
                CurrentHandTrackingMode = localBoneCollecter.CurrentHandTrackingMode;
                StoreRotations(localBoneCollecter.CurrentBoneRotations);
            }
        }

        void StoreRotations(Dictionary<HandSynchronizationBoneId, Quaternion> rotations)
        {
            if (debugDisplayNetworkStateForLocaluser)
            {
                debugStoredRotations.Clear();
                foreach (var rotInfo in rotations) debugStoredRotations[rotInfo.Key] = rotInfo.Value;
            }
            compressedHandState.FillWithQuaternions(rotations, CompressionBonesInfo, maxByteCount: BONE_DATA_SIZE);
            if (compressedHandState.bonesRotationBytes != null)
            {
                for (int i = 0; i < compressedHandState.bonesRotationBytes.Length; i++)
                {
                    CompressedBonesRotations.Set(i, compressedHandState.bonesRotationBytes[i]);
                }
            }
        }

        Dictionary<HandSynchronizationBoneId, Pose> NetworkHandPoses()
        {
            compressedHandState.currentHandTrackingMode = CurrentHandTrackingMode;
            compressedHandState.isDataHighConfidence = IsDataHighConfidence;
            compressedHandState.handScale = HandScale;

            if (CurrentHandTrackingMode == HandTrackingMode.FingerTracking)
            {
                if (compressedHandState.bonesRotationBytes == null || compressedHandState.bonesRotationBytes.Length != CompressedBonesRotations.Length)
                {
                    compressedHandState.bonesRotationBytes = new byte[CompressedBonesRotations.Length];
                }

                for (int i = 0; i < CompressedBonesRotations.Length; i++)
                {
                    compressedHandState.bonesRotationBytes[i] = CompressedBonesRotations[i];
                }
            }

            return UnCompressHandPoses(ref cacheHandState, ref compressedHandState);
        }

        Dictionary<HandSynchronizationBoneId, Pose> NetworkHandPoses(HandTrackingMode currentHandtrackingMode, bool isDataHighConfidence, float handScale, NetworkArrayReadOnly<byte> compressedBoneRotations)
        {
            compressedHandState.currentHandTrackingMode = currentHandtrackingMode;
            compressedHandState.isDataHighConfidence = isDataHighConfidence;
            compressedHandState.handScale = handScale;
            
            if (currentHandtrackingMode == HandTrackingMode.FingerTracking)
            {
                if (compressedHandState.bonesRotationBytes == null || compressedHandState.bonesRotationBytes.Length != compressedBoneRotations.Length)
                {
                    compressedHandState.bonesRotationBytes = new byte[compressedBoneRotations.Length];
                }

                for (int i = 0; i < CompressedBonesRotations.Length; i++)
                {
                    compressedHandState.bonesRotationBytes[i] = compressedBoneRotations[i];
                }
            }

            return UnCompressHandPoses(ref cacheHandState, ref compressedHandState);
        }

        Dictionary<HandSynchronizationBoneId, Pose> UnCompressHandPoses(ref HandState handState, ref CompressedHandState compressedHandState)
        {
            compressedHandState.UncompressToHandState(ref handState, CompressionBonesInfo);
            return PosesWithDefaultPositions(handState.boneRotations);
        }

        Dictionary<HandSynchronizationBoneId, Pose> PosesWithDefaultPositions(Dictionary<HandSynchronizationBoneId, Quaternion> boneRotations)
        {
            var posesByBoneId = new Dictionary<HandSynchronizationBoneId, Pose>();
            foreach (var boneId in boneRotations.Keys)
            {
                var position = Vector3.zero;
                if (defaultBonePositions.bonePositionsByBoneId.ContainsKey(boneId))
                    position = defaultBonePositions.bonePositionsByBoneId[boneId];
                posesByBoneId[boneId] = new Pose { position = position, rotation = boneRotations[boneId] };
            }
            foreach(var boneId in defaultBonePositions.bonePositionsByBoneId.Keys)
            {
                if (posesByBoneId.ContainsKey(boneId))
                {
                    continue;
                }
                Debug.LogError("Missing rotation: "+ boneId);
                var position = defaultBonePositions.bonePositionsByBoneId[boneId];
                posesByBoneId[boneId] = new Pose { position = position, rotation = Quaternion.identity };
            }
            return posesByBoneId;
        }


        public void ApplyPoses(Dictionary<HandSynchronizationBoneId, Pose> posesByboneId)
        {
            bonesReader.ApplyPoses(posesByboneId);
        }


        public override void Render()
        {
            base.Render();
            if (networkHand.IsLocalNetworkRig)
            {
                if (debugDisplayNetworkStateForLocaluser && CurrentHandTrackingMode == HandTrackingMode.FingerTracking)
                {
                    // Analysis of uncomrpession error for fine tuning compression scriptable
                    var poses = NetworkHandPoses();
                    if (debugDisplayCompressionImprovements)
                    {
                        foreach (var poseInfo in poses)
                        {
                            if (debugStoredRotations.ContainsKey(poseInfo.Key) == false)
                            {
                                Debug.LogError("Missing bone in stored info " + poseInfo.Key);
                            }
                            else
                            {
                                var angle = Quaternion.Angle(poseInfo.Value.rotation, debugStoredRotations[poseInfo.Key]);
                                if (angle > 15)
                                {
                                    Debug.LogError($"Uncompression data error: Stored => Restored: " +
                                        $"angle={angle} boneId={poseInfo.Key} " +
                                        $"({poseInfo.Value.rotation.eulerAngles}->{debugStoredRotations[poseInfo.Key].eulerAngles})");
                                }
                            }
                        }
                    }                        
                    ApplyPoses(poses);

                }
                else if (localBoneCollecter != null && bonesReader != null && localBoneCollecter.CurrentHandTrackingMode == HandTrackingMode.FingerTracking)
                {
                    var bonePoses = localBoneCollecter.CurrentBonesPoses;
                    if (bonePoses != null)
                    {
                        if (localBoneCollecter.CurrentBonesPoses.Count == 0)
                            Debug.Log("CurrentBonesPoses not yet initialized: skipping this frame");
                        else
                            ApplyPoses(localBoneCollecter.CurrentBonesPoses);
                    } 
                    else
                    {
                        var poses = PosesWithDefaultPositions(localBoneCollecter.CurrentBoneRotations);
                        ApplyPoses(poses);
                    }
                }
            }
            else
            {
                if (useRotationsInterpolation && TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha))
                {
                    var trackingModeReader = GetPropertyReader<HandTrackingMode>(nameof(CurrentHandTrackingMode));
                    var fromTrackingMode = trackingModeReader.Read(fromBuffer);
                    var toTrackingMode = trackingModeReader.Read(toBuffer);
                    if (fromTrackingMode != HandTrackingMode.FingerTracking || toTrackingMode != HandTrackingMode.FingerTracking)
                    {
                        // No interpolation possible, we return to current state
                        if (CurrentHandTrackingMode == HandTrackingMode.FingerTracking)
                        {
                            ApplyPoses(NetworkHandPoses());
                        }
                        return;
                    }

                    var reader = GetArrayReader<byte>(nameof(CompressedBonesRotations));
                    var fromCompressedBonesRotations = reader.Read(fromBuffer);
                    var toCompressedBonesRotations = reader.Read(toBuffer);

                    var fromPoses = NetworkHandPoses(fromTrackingMode, true, 1f, fromCompressedBonesRotations);
                    var toPoses = NetworkHandPoses(toTrackingMode, true, 1f, toCompressedBonesRotations);

                    Dictionary<HandSynchronizationBoneId, Pose> posesByBoneId = InterpolateBonesRotations(fromPoses, toPoses, alpha);

                    ApplyPoses(posesByBoneId);
                }
                else
                {
                    if (CurrentHandTrackingMode == HandTrackingMode.FingerTracking)
                    {
                        ApplyPoses(NetworkHandPoses());
                    }
                }
            }
        }

        private Dictionary<HandSynchronizationBoneId, Pose> InterpolateBonesRotations(Dictionary<HandSynchronizationBoneId, Pose> fromPoses, Dictionary<HandSynchronizationBoneId, Pose> toPoses, float alpha)
        {
            var posesByBoneId = new Dictionary<HandSynchronizationBoneId, Pose>();
            foreach (var boneId in fromPoses.Keys)
            {
                if (toPoses.ContainsKey(boneId) == false) throw new System.Exception("Unexepected");
                var position = Vector3.zero;
                if (defaultBonePositions.bonePositionsByBoneId.ContainsKey(boneId))
                    position = defaultBonePositions.bonePositionsByBoneId[boneId];
                posesByBoneId[boneId] = new Pose
                {
                    position = position,
                    rotation = Quaternion.Slerp(fromPoses[boneId].rotation, toPoses[boneId].rotation, alpha)
                };
            }

            return posesByBoneId;
        }
    }
}
