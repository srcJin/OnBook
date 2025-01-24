using UnityEngine;
using UnityEngine.XR.Hands;


namespace Fusion.Addons.XRHandsSync
{
    public class FingerDrivenControllerInput : FingerDrivenGesture
    {
        bool hardwareHandDefaultUpdateHandCommandWithAction = false;
        bool hardwareHandDefaultUpdateGrabWithAction = false;

        public bool alwaysKeepHandCommandControl = false;

        [Header("Gesture threshold")]
        [SerializeField]    float pinchThreshold = 0.02f;
        public bool grabWithPinch = true;
        public bool grabWithMiddleAndRing = true;
        float indexFingerPressedThreshold = 0.05f;
        float thumFingerPressedThreshold = 0.05f;
        [SerializeField] float grabbingProximalMinXRot = 65f;
        [SerializeField] float grabbingIntermediateMinXRot = 65f;

        [Header("Grabbing collider")]
        public Collider colliderWhileFingerTracked;
        public Collider colliderWhileNotFingerTracked;

        Pincher pincher;

        protected override void Awake()
        {
            base.Awake();
            if (hardwareHand) pincher = hardwareHand.GetComponentInChildren<Pincher>();
            hardwareHandDefaultUpdateHandCommandWithAction = hardwareHand.updateHandCommandWithAction;
            hardwareHandDefaultUpdateGrabWithAction = hardwareHand.updateGrabWithAction;

        }

        protected override void OnActivateInputControl()
        {
            hardwareHand.updateHandCommandWithAction = false;
            hardwareHand.updateGrabWithAction = false;
            if (colliderWhileFingerTracked) colliderWhileFingerTracked.enabled = true;
            if (colliderWhileNotFingerTracked) colliderWhileNotFingerTracked.enabled = false;
        }

        protected override void OnDesactivateInputcontrol()
        {
            if (alwaysKeepHandCommandControl == false)
            {
                if (hardwareHand.isGrabbing)
                {
                    Debug.Log("Grabbing will stop due to OnDesactivateInputcontrol");
                }
                hardwareHand.updateHandCommandWithAction = hardwareHandDefaultUpdateHandCommandWithAction;
                hardwareHand.updateGrabWithAction = hardwareHandDefaultUpdateGrabWithAction;
            }

            if (colliderWhileFingerTracked) colliderWhileFingerTracked.enabled = false;
            if (colliderWhileNotFingerTracked) colliderWhileNotFingerTracked.enabled = true;
            if (pincher) pincher.OnHandNotPinching();
        }


        public bool grabbing = false;
        bool IsGrabbing(XRHand hand)
        {
            grabbing = IsGrabbing(hand, grabbingProximalMinXRot, grabbingIntermediateMinXRot);
            return grabbing;
        }

        public bool IsGrabbing()
        {
            if (areInputControlled == false) return false;
            return IsGrabbing(lastXRHand);
        }

        public bool IsGrabbing(float grabbingProximalMinXRot, float grabbingIntermediateMinXRot)
        {
            if (areInputControlled == false) return false;
            return IsGrabbing(lastXRHand, grabbingProximalMinXRot, grabbingIntermediateMinXRot);
        }

        public bool IsPinching()
        {
            if (areInputControlled == false) return false;
            var indexTipAvailable = TryGetBonePose(lastXRHand, XRHandJointID.IndexTip, out var indexTipPose, out _);
            var thumbTipAvailable = TryGetBonePose(lastXRHand, XRHandJointID.ThumbTip, out var thumbTipPose, out _);

            if (indexTipAvailable && thumbTipAvailable)
            {
                float indexDistance = Vector3.Distance(indexTipPose.position, thumbTipPose.position);
                if (indexDistance < pinchThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        protected override void UpdatedHands(XRHand hand) {
            hardwareHand.handCommand.pinchCommand = 0;
            hardwareHand.handCommand.gripCommand = 0;
            hardwareHand.handCommand.triggerCommand = 0;
            hardwareHand.handCommand.thumbTouchedCommand = 0;
            bool wasGrabbing = hardwareHand.isGrabbing;
            hardwareHand.isGrabbing = false;
            bool pinching = false;

            var indexTipAvailable = TryGetBonePose(hand, XRHandJointID.IndexTip, out var indexTipPose, out _);
            var thumbTipAvailable = TryGetBonePose(hand, XRHandJointID.ThumbTip, out var thumbTipPose, out _);
            var indexIntermediateAvailable = TryGetBonePose(hand, XRHandJointID.IndexIntermediate, out var indexIntermediatePose, out _);
            var indexProximalAvailable = TryGetBonePose(hand, XRHandJointID.IndexProximal, out var indexProximalPose, out _);
            var palmAvailable = TryGetBonePose(hand, XRHandJointID.Palm, out var palmPose, out var _);


            // Index
            if (indexTipAvailable && palmAvailable)
            {
                var distance = Vector3.Distance(indexTipPose.position, palmPose.position);
                if (distance < indexFingerPressedThreshold)
                {
                    hardwareHand.handCommand.triggerCommand = 1;
                }
            }
            // Thumb
            if (thumbTipAvailable && indexIntermediateAvailable)
            {
                var distance = Vector3.Distance(thumbTipPose.position, indexIntermediatePose.position);
                if (distance < thumFingerPressedThreshold)
                {
                    hardwareHand.handCommand.thumbTouchedCommand = 1;
                }
            }
            if (hardwareHand.handCommand.thumbTouchedCommand == 0)
            {
                if (thumbTipAvailable && indexProximalAvailable)
                {
                    var distance = Vector3.Distance(thumbTipPose.position, indexProximalPose.position);
                    if (distance < thumFingerPressedThreshold)
                    {
                        hardwareHand.handCommand.thumbTouchedCommand = 1;
                    }
                }
            }

            // Pinch
            float indexDistance = 0;
            if (indexTipAvailable && thumbTipAvailable)
            {
                indexDistance = Vector3.Distance(indexTipPose.position, thumbTipPose.position);
                if (indexDistance < pinchThreshold)
                {
                    hardwareHand.handCommand.pinchCommand = 1;
                    //hardwareHand.handCommand.thumbTouchedCommand = 1;
                    //hardwareHand.handCommand.triggerCommand = 0.6f;
                    if (grabWithPinch)
                    {
                        hardwareHand.isGrabbing = true;
                    }
                    pinching = true;
                }
            }
            else
            {
                Debug.LogError($"Missing bone for pitch");
            }


            // Grab
            if (IsGrabbing(hand))
            {
                hardwareHand.handCommand.gripCommand = 1;
                if (grabWithMiddleAndRing)
                {
                    hardwareHand.isGrabbing = true;
                }
            }

            if (pincher)
            {
                if (pinching)
                {
                    pincher.OnHandPinching();
                }
                else
                {
                    pincher.OnHandNotPinching();
                }
            }
        }
    }

}
