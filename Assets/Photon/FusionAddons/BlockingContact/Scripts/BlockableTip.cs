using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared;

namespace Fusion.Addons.BlockingContact
{
    /***
     * 
     * BlockableTip detects trigger contact with objects having their layer in the blockingLayers mask, and having an BlockingSurface component.
     * FixContactPosition() changes grabbable position if needed so that it is not too "deep" in the last contacted surface
     *
     ***/

    [DefaultExecutionOrder(BlockableTip.EXECUTION_ORDER)]
    public class BlockableTip : NetworkBehaviour, IContactHandler
    {
        public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 10;

        BlockingSurface previousSurfaceInContact;
        public BlockingSurface lastSurfaceInContact;
        // Tip (expected to be in the interpolationTarget hierarchy)
        public Transform tip;
        public LayerMask blockingLayers;
        public string additionalBlockingLayerName;
        NetworkGrabbable grabbable;
        public bool IsGrabbed => grabbable && grabbable.IsGrabbed;
        public bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable.CurrentGrabber.Object.StateAuthority == Runner.LocalPlayer;
        // Blockable tip can be used on non grabbable objects
        public virtual bool IsContactAllowed => (grabbable == false) || IsGrabbedBySpatialGrabber || ShouldFixGrabberPosition;
        protected virtual bool ShouldFixBlockablePosition => IsInContact && IsContactAllowed;
        protected virtual bool ShouldFixGrabberPosition => grabbable != null && grabbable.CurrentGrabber && grabbable.CurrentGrabber.hand && IsGrabbed;

        protected virtual bool IsGrabbedBySpatialGrabber {
            get {
#if POLYSPATIAL_SDK_AVAILABLE
                return grabbable != null && grabbable.CurrentGrabber && grabbable.CurrentGrabber is VisionOsHelpers.NetworkSpatialGrabber;
#else
                return false;
#endif
            }
        }

        #region IContactHandler
        public bool IsHandlingContact => ShouldFixBlockablePosition;
        #endregion

        public bool changeActualPosition = false;

        [Header("Feedback")]
        IFeedbackHandler feedback;
        public string audioType = "";
        [SerializeField] float hapticAmplitudeFactor = 0.1f;
        [SerializeField] FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic;

        public float? ContactDepth => ShouldFixBlockablePosition ? lastContactDepth : null;

        float lastContactDepth = 0;

        // Contact in the "referential" referential. If the referential world scale is Vector3.one, the size will be in meter in these coordinates
        public Vector3 ContactCoordinates
        {
            get
            {
                if (lastSurfaceInContact == null) return default;
                var coordinate = lastSurfaceInContact.referential.InverseTransformPoint(tip.position);
                return coordinate;
            }
        }

        // Contact in the actual surface object referential. The sizes of a tip inbound will scale from -0.5 to 0.5 values
        public Vector3 SurfaceContactCoordinates
        {
            get
            {
                if (lastSurfaceInContact == null) return default;
                var coordinate = lastSurfaceInContact.transform.InverseTransformPoint(tip.position);
                return coordinate;
            }
        }

        public bool IsInContact
        {
            get
            {
                if (lastSurfaceInContact == null) return false;
                var coordinate = ContactCoordinates;
                bool isUsed = true;

                // We check if the tip is on surface in the referential of the lastSurfaceInContact
                if (coordinate.z > 0)
                {
                    isUsed = isUsed && coordinate.z < lastSurfaceInContact.positiveProximityThresholds.z;
                }
                else
                {
                    isUsed = isUsed && coordinate.z >= lastSurfaceInContact.negativeProximityThresholds.z;
                }

                if (coordinate.x > 0)
                {
                    isUsed = isUsed && coordinate.x < lastSurfaceInContact.positiveProximityThresholds.x;
                }
                else
                {
                    isUsed = isUsed && coordinate.x >= lastSurfaceInContact.negativeProximityThresholds.x;
                }

                if (coordinate.y > 0)
                {
                    isUsed = isUsed && coordinate.y < lastSurfaceInContact.positiveProximityThresholds.y;
                }
                else
                {
                    isUsed = isUsed && coordinate.y >= lastSurfaceInContact.negativeProximityThresholds.y;
                }

                if (!isUsed)
                {
                    // Stop being in contact: cache the surface in case it is just a temporary lost of contact
                    previousSurfaceInContact = lastSurfaceInContact;
                    lastSurfaceInContact = null;
                }
                return isUsed;
            }
        }

        public float Pressure
        {
            get
            {
                if (lastSurfaceInContact == null) return 0;
                if (lastSurfaceInContact.maxDepth == 0) return 1;
                var coordinate = ContactCoordinates.z;
                return coordinate > 0 ? 1f : Mathf.Abs((lastSurfaceInContact.maxDepth - coordinate) / lastSurfaceInContact.maxDepth);
            }
        }

        private void Awake()
        {
            grabbable = GetComponent<NetworkGrabbable>();
            feedback = GetComponent<IFeedbackHandler>();
            if(string.IsNullOrEmpty(additionalBlockingLayerName) == false)
            {
                int layer = LayerMask.NameToLayer(additionalBlockingLayerName);
                if (layer == -1)
                {
                    Debug.LogError($"Please add a {additionalBlockingLayerName} layer. Required by {gameObject.name}");
                } 
                else
                {
                    blockingLayers |= (1 << layer);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            bool objectInBoardLayerMask = blockingLayers == (blockingLayers | (1 << other.gameObject.layer));
            if (objectInBoardLayerMask)
            {
                // Found a blocking surface
                if (lastSurfaceInContact == null || other.gameObject != lastSurfaceInContact.gameObject)
                {
                    // New blocking surface
                    BlockingSurface surface = null;
                    if (previousSurfaceInContact && previousSurfaceInContact.gameObject == other.gameObject)
                    {
                        // Used a cached previously found BlockingSurface
                        surface = previousSurfaceInContact;
                    }
                    else
                    {
                        // Look for the BlockingSurface component, and cache any previously blocking surface for faster searches
                        previousSurfaceInContact = lastSurfaceInContact;
                        surface = other.gameObject.GetComponent<BlockingSurface>();
                    }

                    if (surface)
                    {
                        lastSurfaceInContact = surface;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (feedbackMode != FeedbackMode.None && feedback != null && previousSurfaceInContact && other.gameObject == previousSurfaceInContact.gameObject)
            {
                feedback.StopAudioFeeback();
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (changeActualPosition)
            {
                FixContactPosition();
            }
        }

        public override void Render()
        {
            base.Render();
            FixContactPosition();
            if (feedbackMode != FeedbackMode.None && feedback != null && ShouldFixBlockablePosition && string.IsNullOrEmpty(audioType) == false)
            {
                feedback.PlayAudioAndHapticFeeback(audioType, Mathf.Clamp01(hapticAmplitudeFactor * lastContactDepth), feedbackMode: feedbackMode);
            }
        }

        /// <summary>
        /// Change grabbable position if needed so that it is not too "deep" in the last contacted surface
        /// </summary>
        protected virtual void FixContactPosition()
        {
            lastContactDepth = 0;
            if (ShouldFixBlockablePosition)
            {
                var tipPositionForBlockableTransform = tip.position;
                var localTipPosition = lastSurfaceInContact.referential.InverseTransformPoint(tipPositionForBlockableTransform);

                lastContactDepth = Mathf.Abs(localTipPosition.z);

                // check if tip Z position exceed the maxDepth
                if (localTipPosition.z > lastSurfaceInContact.maxDepth)
                {

                    localTipPosition.z = lastSurfaceInContact.maxDepth;
                    var newTipPosition = lastSurfaceInContact.referential.TransformPoint(localTipPosition);

                    transform.position = newTipPosition - (tipPositionForBlockableTransform - transform.position);
                }
            }

            if (ShouldFixGrabberPosition)
            {
                Transform handTransform = grabbable.CurrentGrabber.hand.transform;
                handTransform.position = transform.position - (handTransform.TransformPoint(grabbable.LocalPositionOffset) - handTransform.position);
            }
        }
    }
}
