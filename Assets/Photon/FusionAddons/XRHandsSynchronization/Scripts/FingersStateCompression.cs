//#define FINGER_TRACKING_FINE_TUNING

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.Addons.HandsSync
{
    public enum HandTrackingMode
    {
        NotTracked,
        ControllerTracking,
        FingerTracking,
    }

    [System.Serializable]
    public struct BonePosition
    {
        public HandSynchronizationBoneId boneId;
        public Vector3 position;
    }

    [System.Serializable]
    public struct BonePose
    {
        public HandSynchronizationBoneId boneId;
        public Pose pose;
    }

    [System.Serializable]
    public struct HandState
    {
        public const int BONE_COUNT = 24;
        public HandTrackingMode currentHandTrackingMode;
        public bool isDataHighConfidence;
        public float handScale;
        public Dictionary<HandSynchronizationBoneId, Quaternion> boneRotations;

        public void AddBoneRotation(HandSynchronizationBoneId boneId, Quaternion rotation)
        {
            if (boneRotations == null)
            {
                boneRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();
            }
            boneRotations[boneId] = rotation;
        }
    }


    // 0: metacarpal, 1: proximal, 2:intermediate, 3: Distal, Tip
    public enum HandSynchronizationBoneId
    {
        Invalid,
        Hand_WristRoot,
        Hand_ForearmStub,
        Hand_Thumb0, Hand_Thumb1, Hand_Thumb2, Hand_Thumb3, Hand_ThumbTip,
        Hand_Index0, Hand_Index1, Hand_Index2, Hand_Index3, Hand_IndexTip,
        Hand_Middle0, Hand_Middle1, Hand_Middle2, Hand_Middle3, Hand_MiddleTip,
        Hand_Ring0, Hand_Ring1, Hand_Ring2, Hand_Ring3, Hand_RingTip,
        Hand_Pinky0, Hand_Pinky1, Hand_Pinky2, Hand_Pinky3, Hand_PinkyTip,
        Hand_Palm,
    }

    public enum BoneAxisCompressionMode
    {
        HardcodedValue,
        X, Y, Z,
        XY, YZ, XZ,
        XYLowPrecision, YZLowPrecision, XZLowResolution,
        XYZ,
        Quaternion,
        FollowAnotherBone
    }

    [System.Serializable]
    public struct FollowBoneDetails
    {
        public HandSynchronizationBoneId followedBone;
        public CompressedHandState.AxisRot boneAxis;
        public float minInputRot;
        public float maxInputRot;
    }

    [System.Serializable]
    public struct HandBoneInfo
    {
        public HandSynchronizationBoneId boneId;
        public BoneAxisCompressionMode axisCompressionMode;
        public bool applyOffset;
        public Vector3 offsetValue;
        public float XminRange;
        public float XmaxRange;
        public float YminRange;
        public float YmaxRange;
        public float ZminRange;
        public float ZmaxRange;
        public FollowBoneDetails followBoneDetails;

        public int ByteSize
        {
            get
            {
                switch (axisCompressionMode)
                {
                    case BoneAxisCompressionMode.X: case BoneAxisCompressionMode.Y: case BoneAxisCompressionMode.Z: return 1;
                    case BoneAxisCompressionMode.XYLowPrecision: case BoneAxisCompressionMode.YZLowPrecision: case BoneAxisCompressionMode.XZLowResolution: return 1;
                    case BoneAxisCompressionMode.XY: case BoneAxisCompressionMode.YZ: case BoneAxisCompressionMode.XZ: return 2;
                    case BoneAxisCompressionMode.XYZ: return 3; // Might be 2 if we choose to use 1 2-axis and 1 1-axis compression instead of 3 1-axis compression
                    case BoneAxisCompressionMode.Quaternion: return 4 * sizeof(float);
                }
                return 0;
            }
        }

        public void Validate()
        {
            if (XminRange == XmaxRange)
            {
                XminRange = -180f;
                XmaxRange = 180f;
            }
            if (YminRange == YmaxRange)
            {
                YminRange = -180f;
                YmaxRange = 180f;
            }
            if (ZminRange == ZmaxRange)
            {
                ZminRange = -180f;
                ZmaxRange = 180f;
            }
        }
    }

    [System.Serializable]
    public struct CompressedHandState
    {
        public HandTrackingMode currentHandTrackingMode;
        public bool isDataHighConfidence;
        public float handScale;
        public byte[] bonesRotationBytes;

        public enum AxisRot { X, Y, Z }

        // From -180 to 180
        public static float NormalizedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        public static float RotationAxisValue(Quaternion boneLocalRot, AxisRot axis)
        {
            var boneLocalEuler = boneLocalRot.eulerAngles;
            switch (axis)
            {
                case AxisRot.X:
                    return boneLocalEuler.x;
                case AxisRot.Y:
                    return boneLocalEuler.y;
                case AxisRot.Z:
                    return boneLocalEuler.z;
                default:
                    return default;
            }
        }

        //  RemapFloat is used to remap a value from one range to another while maintaining proportionality between the two ranges
        public static float RemapFloat(float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;
            return to;
        }

        // CompressOneBoneRotationIntoOneByte computes the rotation of a bone into a byte.
        public static Byte CompressOneBoneRotationIntoOneByte(Quaternion boneLocalRot, AxisRot axis, float minAngle, float maxAngle, bool debug = false)
        {
            var angle = NormalizedAngle(RotationAxisValue(boneLocalRot, axis));
            if (minAngle != maxAngle) angle = Math.Clamp(angle, minAngle, maxAngle);

            var byteRotation = (byte)RemapFloat(angle, minAngle, maxAngle, 0, 255);
            return byteRotation;
        }

        // CompressTwoBonesRotationIntoOneByte computes the rotation of two bones into a byte.
        public static Byte CompressTwoBonesRotationIntoOneByte(Quaternion boneLocalRot, AxisRot axis1, AxisRot axis2, float minAngle1, float maxAngle1, float minAngle2, float maxAngle2)
        {
            var angle1 = NormalizedAngle(RotationAxisValue(boneLocalRot, axis1));
            var angle2 = NormalizedAngle(RotationAxisValue(boneLocalRot, axis2));
            if (minAngle1 != maxAngle1) angle1 = Math.Clamp(angle1, minAngle1, maxAngle1);
            if (minAngle2 != maxAngle2) angle2 = Math.Clamp(angle2, minAngle2, maxAngle2);

            var byteRotation1 = (byte)RemapFloat(angle1, minAngle1, maxAngle1, 0, 15);
            var byteRotation2 = (byte)RemapFloat(angle2, minAngle2, maxAngle2, 0, 15);

            byteRotation1 = (byte)(byteRotation1 << 4);


            return (byte)(byteRotation1 | byteRotation2);
        }

        // DecompressTwoBonesRotationFromOneByte computes the rotation of two bones (in a single byte) into a Quaternion.
        public static Quaternion DecompressTwoBonesRotationFromOneByte(Byte compressedValue,
            AxisRot axis1, AxisRot axis2, float minAngle1, float maxAngle1, float minAngle2, float maxAngle2)
        {
            var decompressed2 = (compressedValue & 15);
            var decompressed1 = compressedValue >> 4;

            var amount1 = RemapFloat(decompressed1, 0, 15, minAngle1, maxAngle1);
            var amount2 = RemapFloat(decompressed2, 0, 15, minAngle2, maxAngle2);

            float x = 0, y = 0, z = 0;

            switch (axis1)
            {
                case AxisRot.X:
                    x = amount1;
                    break;
                case AxisRot.Y:
                    y = amount1;
                    break;
                case AxisRot.Z:
                    z = amount1;
                    break;
            }

            switch (axis2)
            {
                case AxisRot.X:
                    x = amount2;
                    break;
                case AxisRot.Y:
                    y = amount2;
                    break;
                case AxisRot.Z:
                    z = amount2;
                    break;
            }

            var result = Quaternion.Euler(x, y, z);

            return result;
        }

        // ComputeLastPhalanx computes the axis rotation of a bone based on another bone rotation.
        // It is used to reduce the number of byte to synchronize on the network. Indeed, most of time, the angle of the last phalanx is proportional to the other phalanx.
        // The calculation follows a parabolic law similar to: followingBoneRot = a * followedBoneRot?
        // "a" being computed so that:
        // If input angle=10?, output angle=1?
        // If input angle=45?, output angle=20?
        // If input angle=90?, output angle=81?
        public static Quaternion ComputeLastPhalanx(Quaternion inputQuaternion, AxisRot axisRot, float minInputRot, float maxInputRot)
        {
            Vector3 euler = inputQuaternion.eulerAngles;


            // Ensure values are within -180 to 180 range
            if (euler.x > 180)
                euler.x -= 360;
            if (euler.y > 180)
                euler.y -= 360;
            if (euler.z > 180)
                euler.z -= 360;

            if (minInputRot != 0 || maxInputRot != 0)
            {
                switch (axisRot)
                {
                    case AxisRot.X: euler.x = Mathf.Clamp(euler.x, minInputRot, maxInputRot); break;
                    case AxisRot.Y: euler.y = Mathf.Clamp(euler.y, minInputRot, maxInputRot); break;
                    case AxisRot.Z: euler.z = Mathf.Clamp(euler.z, minInputRot, maxInputRot); break;
                }

            }

            Vector3 newEuler = euler;

            // Parrabola adaptation of value: 
            float a = 0.01f;

            switch (axisRot)
            {
                case AxisRot.X:
                    newEuler.x = a * euler.x * euler.x * Mathf.Sign(euler.x);
                    break;

                case AxisRot.Y:
                    newEuler.y = a * euler.x * euler.y * Mathf.Sign(euler.y);
                    break;

                case AxisRot.Z:
                    newEuler.z = a * euler.z * euler.z * Mathf.Sign(euler.z);
                    break;
            }
            Quaternion modifiedQuaternion = Quaternion.Euler(newEuler);
            return modifiedQuaternion;
        }


        // FillWithQuaternions updates the HandState bonesRotations byte array based on the rotationByBone actual data
        // The HandBoneInfo list parameter defined how each bones must be compressed.
        public void FillWithQuaternions(Dictionary<HandSynchronizationBoneId, Quaternion> rotationByBone, List<HandBoneInfo> bonesInfo, int maxByteCount)
        {
            int index = 0;
            if (bonesRotationBytes == null || bonesRotationBytes.Length != maxByteCount)
            {
                bonesRotationBytes = new byte[maxByteCount];
            }
            foreach (var handBoneInfo in bonesInfo)
            {
                handBoneInfo.Validate();
                HandSynchronizationBoneId boneId = handBoneInfo.boneId;

                if (boneId != HandSynchronizationBoneId.Invalid)
                {
                    if ((index + handBoneInfo.ByteSize) > maxByteCount)
                    {
                        int byteSize = 0;
                        if (bonesInfo != null) foreach (var b in bonesInfo) byteSize += b.ByteSize;
                        throw new Exception($"Target byte storage ({maxByteCount} bytes) too small for current compression ({byteSize} bytes required) ");
                    }

                    var boneRotation = rotationByBone.ContainsKey(boneId) ? rotationByBone[boneId] : Quaternion.identity;
                    switch (handBoneInfo.axisCompressionMode)
                    {
                        case BoneAxisCompressionMode.HardcodedValue:
                            // no need to sync harcoded value
                            break;

                        case BoneAxisCompressionMode.X:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.X, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                            break;

                        case BoneAxisCompressionMode.Y:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Y, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                            break;

                        case BoneAxisCompressionMode.Z:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Z, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.XYLowPrecision:
                            var angle1 = NormalizedAngle(RotationAxisValue(boneRotation, AxisRot.X));
                            var angle2 = NormalizedAngle(RotationAxisValue(boneRotation, AxisRot.Y));
                            angle1 = Math.Clamp(angle1, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                            angle2 = Math.Clamp(angle2, handBoneInfo.YminRange, handBoneInfo.YmaxRange);

                            var byteRotation1 = (byte)RemapFloat(angle1, handBoneInfo.XminRange, handBoneInfo.XmaxRange, 0, 15);
                            var byteRotation2 = (byte)RemapFloat(angle2, handBoneInfo.YminRange, handBoneInfo.YmaxRange, 0, 15);

                            bonesRotationBytes[index] = CompressTwoBonesRotationIntoOneByte(boneRotation, AxisRot.X, AxisRot.Y, handBoneInfo.XminRange, handBoneInfo.XmaxRange, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                            break;

                        case BoneAxisCompressionMode.YZLowPrecision:
                            bonesRotationBytes[index] = CompressTwoBonesRotationIntoOneByte(boneRotation, AxisRot.Y, AxisRot.Z, handBoneInfo.YminRange, handBoneInfo.YmaxRange, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.XZLowResolution:
                            bonesRotationBytes[index] = CompressTwoBonesRotationIntoOneByte(boneRotation, AxisRot.X, AxisRot.Z, handBoneInfo.XminRange, handBoneInfo.XmaxRange, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.XY:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.X, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                            bonesRotationBytes[index + 1] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Y, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                            break;

                        case BoneAxisCompressionMode.YZ:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Y, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                            bonesRotationBytes[index + 1] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Z, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.XZ:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.X, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                            bonesRotationBytes[index + 1] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Z, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.XYZ:
                            bonesRotationBytes[index] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.X, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                            bonesRotationBytes[index + 1] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Y, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                            bonesRotationBytes[index + 2] = CompressOneBoneRotationIntoOneByte(boneRotation, AxisRot.Z, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                            break;

                        case BoneAxisCompressionMode.Quaternion:
                            byte[] quaternionArray = QuaternionSerialization.AsByteArray(boneRotation);
                            Array.Copy(quaternionArray, 0, bonesRotationBytes, index, quaternionArray.Length);
                            break;

                        case BoneAxisCompressionMode.FollowAnotherBone:
                            // no need to sync
                            break;
                    }
                    index += handBoneInfo.ByteSize;
                }
            }
        }

        // UncompressToHandState updates the HandState with and the HandBoneInfo list received in parameter
        public void UncompressToHandState(ref HandState handState, List<HandBoneInfo> bonesInfo)
        {
            handState.currentHandTrackingMode = currentHandTrackingMode;
            handState.isDataHighConfidence = isDataHighConfidence;
            handState.handScale = handScale;

            if (handState.boneRotations == null)
            {
                handState.boneRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();
            }
            handState.boneRotations.Clear();

            if (currentHandTrackingMode != HandTrackingMode.FingerTracking)
            {
                // No need to change the bone rotations: no valid finger tracking data
                return;
            }

            int index = 0;

            if (bonesRotationBytes == null)
            {
                Debug.LogError("Bad compressed state");
            }
            else
            {
                foreach (var handBoneInfo in bonesInfo)
                {
                    handBoneInfo.Validate();
                    HandSynchronizationBoneId boneId = handBoneInfo.boneId;
                    bool applyOffset = handBoneInfo.applyOffset;

                    if (applyOffset)
                    {
                        handState.boneRotations[boneId] = Quaternion.Euler(handBoneInfo.offsetValue);
                    }

                    if (boneId != HandSynchronizationBoneId.Invalid)
                    {
                        if ((index + handBoneInfo.ByteSize) > bonesRotationBytes.Length)
                        {
                            int byteSize = 0;
                            if (bonesInfo != null) foreach (var b in bonesInfo) byteSize += b.ByteSize;
                            throw new Exception($"Target byte storage ({bonesRotationBytes.Length} bytes) too small for current uncompression ({byteSize} bytes required) ");
                        }
                        Quaternion rotation = Quaternion.identity;

                        switch (handBoneInfo.axisCompressionMode)
                        {
                            case BoneAxisCompressionMode.HardcodedValue:
                                break;

                            case BoneAxisCompressionMode.X:
                                rotation = Quaternion.Euler(
                                    RemapFloat(bonesRotationBytes[index], 0, 255, handBoneInfo.XminRange, handBoneInfo.XmaxRange),
                                    0,
                                    0
                                );
                                break;

                            case BoneAxisCompressionMode.Y:
                                rotation = Quaternion.Euler(
                                    0,
                                    RemapFloat(bonesRotationBytes[index + 1], 0, 255, handBoneInfo.YminRange, handBoneInfo.YmaxRange),
                                    0
                                );
                                break;

                            case BoneAxisCompressionMode.Z:
                                rotation = Quaternion.Euler(
                                    0,
                                    0,
                                    RemapFloat(bonesRotationBytes[index + 2], 0, 255, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange)
                                );
                                break;

                            case BoneAxisCompressionMode.XYLowPrecision:
                                var decompressed2 = (bonesRotationBytes[index] & 15);
                                var decompressed1 = bonesRotationBytes[index] >> 4;
                                var amount1 = RemapFloat(decompressed1, 0, 15, handBoneInfo.XminRange, handBoneInfo.XmaxRange);
                                var amount2 = RemapFloat(decompressed2, 0, 15, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                                rotation = DecompressTwoBonesRotationFromOneByte(bonesRotationBytes[index], AxisRot.X, AxisRot.Y, handBoneInfo.XminRange, handBoneInfo.XmaxRange, handBoneInfo.YminRange, handBoneInfo.YmaxRange);
                                break;

                            case BoneAxisCompressionMode.YZLowPrecision:
                                rotation = DecompressTwoBonesRotationFromOneByte(bonesRotationBytes[index], AxisRot.Y, AxisRot.Z, handBoneInfo.YminRange, handBoneInfo.YmaxRange, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                                break;

                            case BoneAxisCompressionMode.XZLowResolution:
                                rotation = DecompressTwoBonesRotationFromOneByte(bonesRotationBytes[index], AxisRot.X, AxisRot.Z, handBoneInfo.XminRange, handBoneInfo.XmaxRange, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange);
                                break;

                            case BoneAxisCompressionMode.XY:
                                rotation = Quaternion.Euler(
                                    RemapFloat(bonesRotationBytes[index], 0, 255, handBoneInfo.XminRange, handBoneInfo.XmaxRange),
                                    RemapFloat(bonesRotationBytes[index + 1], 0, 255, handBoneInfo.YminRange, handBoneInfo.YmaxRange),
                                    0
                                );

                                break;
                            case BoneAxisCompressionMode.YZ:
                                rotation = Quaternion.Euler(
                                    0,
                                    RemapFloat(bonesRotationBytes[index], 0, 255, handBoneInfo.YminRange, handBoneInfo.YmaxRange),
                                    RemapFloat(bonesRotationBytes[index + 1], 0, 255, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange)
                                );

                                break;
                            case BoneAxisCompressionMode.XZ:
                                rotation = Quaternion.Euler(
                                    RemapFloat(bonesRotationBytes[index], 0, 255, handBoneInfo.XminRange, handBoneInfo.XmaxRange),
                                    0,
                                    RemapFloat(bonesRotationBytes[index + 1], 0, 255, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange)
                                );

                                break;
                            case BoneAxisCompressionMode.XYZ:
                                rotation = Quaternion.Euler(
                                    RemapFloat(bonesRotationBytes[index], 0, 255, handBoneInfo.XminRange, handBoneInfo.XmaxRange),
                                    RemapFloat(bonesRotationBytes[index + 1], 0, 255, handBoneInfo.YminRange, handBoneInfo.YmaxRange),
                                    RemapFloat(bonesRotationBytes[index + 2], 0, 255, handBoneInfo.ZminRange, handBoneInfo.ZmaxRange)
                                );
                                break;

                            case BoneAxisCompressionMode.Quaternion:
                                QuaternionSerialization.Unserialize(bonesRotationBytes, ref index, out rotation);
                                break;

                            case BoneAxisCompressionMode.FollowAnotherBone:
                                if (handState.boneRotations.ContainsKey(handBoneInfo.followBoneDetails.followedBone) == false)
                                {
                                    throw new Exception($"Error in uncompress: {boneId} FollowAnotherBone {handBoneInfo.followBoneDetails.followedBone}, with this one not already computed. Order the compression description so that {handBoneInfo.followBoneDetails.followedBone} is uncompressed first");
                                }
                                var followedBoneRotation = handState.boneRotations[handBoneInfo.followBoneDetails.followedBone];
                                rotation = ComputeLastPhalanx(followedBoneRotation, handBoneInfo.followBoneDetails.boneAxis, handBoneInfo.followBoneDetails.minInputRot, handBoneInfo.followBoneDetails.maxInputRot);
                                break;
                        }

                        if (applyOffset)
                            handState.boneRotations[boneId] *= rotation;
                        else
                            handState.boneRotations[boneId] = rotation;

                        if (handBoneInfo.axisCompressionMode != BoneAxisCompressionMode.Quaternion)
                        {
                            // for Quaternion, the QuaternionSerialization.Unserialize already handle the index incrementation
                            index += handBoneInfo.ByteSize;
                        }
                    }
                }
            }
        }
    }

    public static class QuaternionSerialization
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out Quaternion q)
        {
            q = default;
            int size = SerializationSize(q);
            var objectBytes = new byte[size];
            Unserialize(dataBytes, ref unserializePosition, out q.x);
            Unserialize(dataBytes, ref unserializePosition, out q.y);
            Unserialize(dataBytes, ref unserializePosition, out q.z);
            Unserialize(dataBytes, ref unserializePosition, out q.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out float f)
        {
            f = default;
            int size = SerializationSize(f);
            if ((unserializePosition + size) > dataBytes.Length)
            {
                throw new Exception("Unserialize trying to recover too much data");
            }
            var objectBytes = new byte[size];
            System.Buffer.BlockCopy(dataBytes, unserializePosition, objectBytes, 0, size);
            f = System.BitConverter.ToSingle(objectBytes);
            unserializePosition += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(Quaternion q)
        {
            var repr = new byte[SerializationSize(q)];
            System.Buffer.BlockCopy(AsByteArray(q.x), 0, repr, 0, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.y), 0, repr, sizeof(float), sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.z), 0, repr, sizeof(float) * 2, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.w), 0, repr, sizeof(float) * 3, sizeof(float));
            return repr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(float f)
        {
            return sizeof(float);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(Quaternion q)
        {
            return sizeof(float) * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(float f)
        {
            return System.BitConverter.GetBytes(f);
        }
    }
}
