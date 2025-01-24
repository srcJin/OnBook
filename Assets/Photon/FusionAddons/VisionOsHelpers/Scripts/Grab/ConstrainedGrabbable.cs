using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Fusion.Addons.VisionOsHelpers
{
    /**
     * 
     * ConstrainedGrabbable class should be used to move objects with the constraint of head position to set proper rotation.
     * 
     **/

    public class ConstrainedGrabbable : Grabbable
    {
        HardwareRig hardwareRig;

        public enum ConstraintType
        {
            RotationFilter,
            LookAtHeadset
        }
        public ConstraintType constraintType = ConstraintType.LookAtHeadset;

        public Vector3 rotationConstraint = new Vector3(0, 1, 0);
        public bool invertLookAtHeadsetDirection = false;
        [Tooltip("The collider used on the grabber. Note that if 2 colliders on the grabber were in contact during the grab, it might not be possible to find the proper one")]
        public Collider grabCollider;
        protected override void Awake()
        {
            base.Awake();
            if (GetComponentInChildren<Rigidbody>() == null)
            {
                Debug.LogError("Constrained grabbable required a rigidbody to find the graber collider used");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (currentGrabber != null && grabCollider == null)
            {
                Grabber grabber = other.GetComponentInParent<Grabber>();
                if (grabber != null && grabber == currentGrabber)
                {
                    grabCollider = other;
                }
            }
        }

        public override void Ungrab()
        {
            base.Ungrab();
            grabCollider = null;
        }

        public override void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            base.Follow(followedTransform, localPositionOffsetToFollowed, localRotationOffsetTofollowed);
            var currentRotation = transform.rotation;
            Vector3 grabbingPointCurrentPosition;
            if (grabCollider)
            {
                // Note: even if grabCollider is determined, it is not the exact grabbing position (we can't have the precise contact point with trigger collider only)
                grabbingPointCurrentPosition = grabCollider.transform.position;
            }
            else
            {
                grabbingPointCurrentPosition = followedTransform.position;
            }

            var localGrappingPointPositionForGrabbableTransform = transform.InverseTransformPoint(grabbingPointCurrentPosition);
            if (constraintType == ConstraintType.RotationFilter)
            {
                transform.rotation = Quaternion.Euler(
                    rotationConstraint.x == 0 ? 0 : currentRotation.eulerAngles.x,
                    rotationConstraint.y == 0 ? 0 : currentRotation.eulerAngles.y,
                    rotationConstraint.z == 0 ? 0 : currentRotation.eulerAngles.z
                   );
            }
            else
            {
                if (hardwareRig == null) hardwareRig = currentGrabber.GetComponentInParent<HardwareRig>();
                if (hardwareRig == null) {
                    // Probably using the spatial grbbing (no parent rig)
                    hardwareRig = FindObjectOfType<HardwareRig>();
                }
                if (hardwareRig)
                {
                    var direction = hardwareRig.headset.transform.position - transform.position;
                    if (invertLookAtHeadsetDirection)
                    {
                        direction = -direction;
                    }
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            var newGrabbbingPointPosition = transform.TransformPoint(localGrappingPointPositionForGrabbableTransform);
            transform.position = transform.position - (newGrabbbingPointPosition - grabbingPointCurrentPosition);
        }
    }

}
