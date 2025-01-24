
using UnityEngine;

namespace Fusion.Addon.Colocalization
{
    /***
     * 
     * The MixedRealityRelocalization class provides geometry methods for AR relocalization
     * 
     ***/
    public static class MixedRealityRelocalization
    {
        // Returns the new rig position, so that the headset position relatively to the localAnchor becomes the new headset position relatively to the targetAnchor
        //  (in AR, the localAnchor reallife position will have the targetAnchor corrdinates after the teleport)
        static public (Vector3 newRigPosition, Quaternion newRigRotation) NewRigPositionToMoveAnchorToTarget(Vector3 localAnchorPosition, Quaternion localAnchorRotation, Vector3 targetAnchorPosition, Quaternion targetAnchorRotation, Transform rigTransform, Transform headsetTransform, bool ignoreYAxisMove = true)
        {
            Matrix4x4 localAnchorTransformMatrix = Matrix4x4.TRS(localAnchorPosition, localAnchorRotation, Vector3.one);
            Matrix4x4 targetAnchorTransformMatrix = Matrix4x4.TRS(targetAnchorPosition, targetAnchorRotation, Vector3.one);

            // Equivalent of localAnchorTransform.InverseTransformPoint(hardwareRig.headset.transform.position)
            Vector3 headsetPositionInCandidateReferential = localAnchorTransformMatrix.inverse.MultiplyPoint3x4(headsetTransform.position);
            var headsetRotationInCandidateReferential = Quaternion.Inverse(localAnchorRotation) * headsetTransform.rotation;

            var newHeadsetPosition = targetAnchorTransformMatrix.MultiplyPoint(headsetPositionInCandidateReferential);
            //var newHeadsetPosition = targetAnchorTransform.TransformPoint(headsetPositionInCandidateReferential);
            var newHeadsetRotation = targetAnchorRotation * headsetRotationInCandidateReferential;

            var headsetLocalPosition = rigTransform.InverseTransformPoint(headsetTransform.position);
            var headsetLocalRotation = Quaternion.Inverse(rigTransform.rotation) * headsetTransform.rotation;
            (var newRigPosition, var newRigRotation) = ReferentialPositionToRespectChildPositionOffset(rigTransform, newHeadsetPosition, newHeadsetRotation, headsetLocalPosition, headsetLocalRotation);
            // We don't adapt on the vertical axis
            if (ignoreYAxisMove)
            {
                newRigPosition.y = rigTransform.position.y;
            }

            return (newRigPosition, newRigRotation);
        }

        // Return the position and rotation of a referential referenceTransform so that a offsetedTransform already placed properly will have the required positionOffset/rotationOffset
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectChildPositionOffset(Transform referenceTransform, Vector3 offsetedTransformPosition, Quaternion offsetedTransformRotation, Vector3 positionOffset, Quaternion rotationOffset)
        {
            var rotation = offsetedTransformRotation * Quaternion.Inverse(rotationOffset);
            // We do not apply the rotation to the transform right now, so to use the rotated transform, we can't rely on it and have to use a matrix to emulate in advance the new transform position
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, rotation, referenceTransform.localScale);
            // If the transform was already rotated, it would be equivalent to Equivalent to:
            //     var offsetInRotatedReference = referenceTransform.TransformPoint(positionOffset);
            var offsetInRotatedReference = referenceTransformMatrix.MultiplyPoint(positionOffset);
            var position = offsetedTransformPosition - (offsetInRotatedReference - referenceTransform.transform.position);
            return (position, rotation);
        }
    }

}
