using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using UnityEngine.Events;
using Fusion.XR.Shared;

namespace Fusion.XRShared.GrabbableMagnet
{
    [DefaultExecutionOrder(MagnetPoint.EXECUTION_ORDER)]
    public class MagnetPoint : NetworkBehaviour, IAttracktableMagnet
    {
        public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 5;
        [HideInInspector]
        public NetworkTRSP rootNTRSP;
        NetworkGrabbable networkGrabbable;
        Rigidbody rb;

        [Header("Snap options as an attracted")]
        public AlignmentAxisAsAttracted alignmentAxisAsAttracted = AlignmentAxisAsAttracted.Y;
        public float magnetRadius = 0.1f;
        public LayerMask compatibleLayers;
        public string additionalCompatibleLayer = "";
        public bool addObjectLayerToCompatibleLayers = true;

        [Header("Attractor options")]
        public AlignmentAxisAsAttractor alignmentAxisAsAttractor = AlignmentAxisAsAttractor.MinusY;
        [Tooltip("AttracktOnlyOnAlignmentAxis: The attracted magnet will only move to project itself on the plane defined by the attractor magnet alignment axis" +
            "\nMatchAttracktingMagnetPosition : the attracted magnet will match the attractork magnet position")]
        public AttractedMagnetMove attractedMagnetMove = AttractedMagnetMove.MatchAttracktingMagnetPosition;
        [Tooltip("MatchAlignmentAxis: The attracted object will rotate only to align the attracted axis and the attractor axis\nMatchAlignmentAxisWithOrthogonalRotation: The attracted object will also rotate to only have 90 angles between other axis")]
        public AttractedMagnetRotation attractedMagnetRotation = AttractedMagnetRotation.MatchAlignmentAxisWithOrthogonalRotation;

        [Header("Snap animation")]
        public bool instantSnap = true;
        public float snapDuration = 1;

        [Header("Automatic layer setup")]
        [Tooltip("If set, this object and its children collider will be set to this layer")]
        public string magnetLayer = "Magnets";

        public bool CheckOnUngrab { get; set; } = true;

        MagnetCoordinator _magnetCoordinator;
        public MagnetCoordinator MagnetCoordinator => _magnetCoordinator;


        public UnityEvent onSnapToMagnet;

        [Header("Proximity detection while grabbing")]
        public bool enableProximityDetectionWhileGrabbed = false;
        public UnityEvent<IMagnet, IMagnet, float> onMagnetDetectedInProximity = null;
        public UnityEvent<IMagnet, IMagnet, float> onMagnetProximity = null;
        public UnityEvent<IMagnet, IMagnet> onMagnetLeavingProximity = null;
        IMagnet proximityMagnet = null;

        protected bool IsGrabbed => networkGrabbable && networkGrabbable.IsGrabbed;

        [Header("Feedback")]
        [SerializeField] IFeedbackHandler feedback;
        [SerializeField] string audioType;

        #region IAttracktableMagnet
        public AlignmentAxisAsAttracted AlignmentAxisAsAttracted
        {
            get => alignmentAxisAsAttracted;
            set => alignmentAxisAsAttracted = value;
        }

        public AlignmentAxisAsAttractor AlignmentAxisAsAttractor
        {
            get => alignmentAxisAsAttractor;
            set => alignmentAxisAsAttractor = value;
        }

        public AttractedMagnetMove AttractedMagnetMove
        {
            get => attractedMagnetMove;
            set => attractedMagnetMove = value;
        }
        public AttractedMagnetRotation AttractedMagnetRotation
        {
            get => attractedMagnetRotation;
            set => attractedMagnetRotation = value;
        }
        public float MagnetRadius
        {
            get => magnetRadius;
            set => magnetRadius = value;
        }
        #endregion


        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (snapRequest != null && IsGrabbed)
            {
                // Cancel snap
                snapRequest = null;
            }
            if (snapRequest != null)
            {
                DoSnapToMagnet(snapRequest);
            }
        }

        private void Awake()
        {
            _magnetCoordinator = GetComponentInParent<MagnetCoordinator>();

            rootNTRSP = GetComponentInParent<NetworkTRSP>();
            networkGrabbable = GetComponentInParent<NetworkGrabbable>();
            rb = GetComponentInParent<Rigidbody>();
            if(networkGrabbable) networkGrabbable.onDidUngrab.AddListener(OnDidUngrab);
            if (string.IsNullOrEmpty(magnetLayer) == false)
            {
                int layer = LayerMask.NameToLayer(magnetLayer);
                if (layer == -1)
                {
                    Debug.LogError($"Please add a {magnetLayer} layer (it will be automatically be set to this object)");
                }
                else
                {
                    gameObject.layer = layer;
                    foreach (var collider in GetComponentsInChildren<Collider>())
                    {
                        collider.gameObject.layer = layer;
                    }
                }
            }
            if (string.IsNullOrEmpty(additionalCompatibleLayer) == false)
            {
                int layer = LayerMask.NameToLayer(additionalCompatibleLayer);
                if (layer == -1)
                {
                    Debug.LogError($"Please add a {magnetLayer} layer (it will be automatically be set to this object magnet mask)");
                }
                else
                {
                    compatibleLayers |= (1 << layer);
                }
            }

            if (feedback == null)
                feedback = GetComponent<IFeedbackHandler>();
        }

        private void OnDidUngrab()
        {
            if (CheckOnUngrab)
            {
                CheckMagnetProximity();
            }
        }

        public bool TryFindClosestMagnetInRange(out IMagnet closestMagnet, out float minDistance)
        {
            var layerMask = compatibleLayers;
            if (addObjectLayerToCompatibleLayers) {
                layerMask  = layerMask  | (1 << gameObject.layer);
            }
            var colliders = Physics.OverlapSphere(transform.position, magnetRadius, layerMask: layerMask);

            closestMagnet = null;
            minDistance = magnetRadius;
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                IMagnet magnet = collider.GetComponentInParent<IMagnet>();
                if (magnet == null)
                {
                    Debug.LogError($"No magnet ({collider})");
                    continue;
                }
                if((Object)magnet == this)
                {
                    continue;
                }
                if(magnet is IAttracktableMagnet movableMagnet)
                {
                    if (MagnetCoordinator != null && movableMagnet.MagnetCoordinator == MagnetCoordinator)
                    {
                        continue;
                    }
                }

                var distance = Vector3.Distance(transform.position, magnet.SnapTargetPosition(transform.position));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestMagnet = magnet;
                }
            }
            return closestMagnet != null;
        }

        [ContextMenu("CheckMagnetProximity")]
        public void CheckMagnetProximity()
        {
            if (Object && Object.HasStateAuthority && IsGrabbed == false)
            {

                if (TryFindClosestMagnetInRange(out var closestMagnet, out _))
                {
                    SnapToMagnet(closestMagnet);
                }
            }
        }

        IMagnet snapRequest = null;
        float snapStart = -1;

        public void SnapToMagnet(IMagnet magnet)
        {
            snapRequest = magnet;
            snapStart = Time.time;
        }

        public void DoSnapToMagnet(IMagnet magnet)
        {
            float progress = 1;
            if (instantSnap)
            {
                snapRequest = null;
            }
            else
            {
                progress = (Time.time - snapStart) / snapDuration;
                if(progress >= 1)
                {
                    progress = 1;
                    snapRequest = null;
                }
            }
            if (rb && rb.isKinematic == false)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Rotate the parent NT to match the magnet positions
            Quaternion targetRotation;
            if (magnet.AttractedMagnetRotation == AttractedMagnetRotation.MatchAlignmentAxis)
            {
                targetRotation = AdaptedRotationOnAlignAxis(magnet.transform, magnet.AlignmentAxisAsAttractor);
            }
            else
            {
                targetRotation = AdaptedRotationOnAllAxis(magnet.transform, magnet.AlignmentAxisAsAttractor);
            }
            ApplyRotation(targetRotation, progress);

            // Move the parent NT to match the magnet positions
            var targetPosition = magnet.SnapTargetPosition(transform.position);
            ApplyPosition(targetPosition, progress);

            // Send event
            if (onSnapToMagnet != null)
            {
                onSnapToMagnet.Invoke();

                if (feedback != null)
                {
                    feedback.PlayAudioFeeback(audioType);
                }
            }
        }

        public Vector3 SnapTargetPosition(Vector3 position)
        {
            if (attractedMagnetMove == AttractedMagnetMove.AttracktOnlyOnAlignmentAxis)
            {
                var planeDirection = Vector3.zero;

                switch (alignmentAxisAsAttractor)
                {
                    case AlignmentAxisAsAttractor.Y:
                    case AlignmentAxisAsAttractor.MinusY:
                    case AlignmentAxisAsAttractor.AnyY:
                        planeDirection = transform.up;
                        break;
                    case AlignmentAxisAsAttractor.Z:
                    case AlignmentAxisAsAttractor.MinusZ:
                    case AlignmentAxisAsAttractor.AnyZ:
                        planeDirection = transform.forward;
                        break;
                    case AlignmentAxisAsAttractor.X:
                    case AlignmentAxisAsAttractor.MinusX:
                    case AlignmentAxisAsAttractor.AnyX:
                        planeDirection = transform.right;
                        break;
                }

                var projectionPlane = new Plane(planeDirection, transform.position);
                // Project position on plane
                return projectionPlane.ClosestPointOnPlane(position);
            }
            else
            {
                return transform.position;
            }
        }

        protected virtual Quaternion AdaptedRotationOnAlignAxis(Transform targetTransform, AlignmentAxisAsAttractor targetAlignAxisAsAttractor)
        {
            return AdaptedRotation(targetTransform, targetAlignAxisAsAttractor, useTargetPlaneAxisOrthogonalGuide: false);
        }

        // Find the most appropriate axis to adapt on the align axis while aligning other axis too
        protected virtual Quaternion AdaptedRotationOnAllAxis(Transform targetTransform, AlignmentAxisAsAttractor targetAlignAxisAsAttractor)
        {
            return AdaptedRotation(targetTransform, targetAlignAxisAsAttractor, useTargetPlaneAxisOrthogonalGuide: true);
        }

        protected virtual Quaternion AdaptedRotation(Transform targetTransform, AlignmentAxisAsAttractor targetAlignAxisAsAttractor, bool useTargetPlaneAxisOrthogonalGuide = false)
        {
            var sourceAlignementAxis = Vector3.zero;
            var sourceAlignementPlaneAxis = Vector3.zero;

            switch (alignmentAxisAsAttracted)
            {
                case AlignmentAxisAsAttracted.Y:
                    sourceAlignementAxis = transform.up;
                    sourceAlignementPlaneAxis = transform.forward;
                    break;
                case AlignmentAxisAsAttracted.MinusY:
                    sourceAlignementAxis = -transform.up;
                    sourceAlignementPlaneAxis = transform.forward;
                    break;
                case AlignmentAxisAsAttracted.Z:
                    sourceAlignementAxis = transform.forward;
                    sourceAlignementPlaneAxis = transform.up;
                    break;
                case AlignmentAxisAsAttracted.MinusZ:
                    sourceAlignementAxis = -transform.forward;
                    sourceAlignementPlaneAxis = transform.up;
                    break;
                case AlignmentAxisAsAttracted.X:
                    sourceAlignementAxis = transform.right;
                    sourceAlignementPlaneAxis = transform.up;
                    break;
                case AlignmentAxisAsAttracted.MinusX:
                    sourceAlignementAxis = -transform.right;
                    sourceAlignementPlaneAxis = transform.up;
                    break;

            }

            var targetAlignmentAxis = Vector3.zero;
            var targetAlignmentPlaneVector = Vector3.zero;
            switch (targetAlignAxisAsAttractor)
            {
                case AlignmentAxisAsAttractor.Y:
                    targetAlignmentAxis = targetTransform.up;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.up);
                    break;
                case AlignmentAxisAsAttractor.MinusY:
                    targetAlignmentAxis = -targetTransform.up;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.up);
                    break;
                case AlignmentAxisAsAttractor.AnyY:
                    targetAlignmentAxis = Vector3.Project(sourceAlignementAxis, targetTransform.up);
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.up);
                    break;
                case AlignmentAxisAsAttractor.Z:
                    targetAlignmentAxis = targetTransform.forward;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.forward);
                    break;
                case AlignmentAxisAsAttractor.MinusZ:
                    targetAlignmentAxis = -targetTransform.forward;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.forward);
                    break;
                case AlignmentAxisAsAttractor.AnyZ:
                    targetAlignmentAxis = Vector3.Project(sourceAlignementAxis, targetTransform.forward);
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.forward);
                    break;
                case AlignmentAxisAsAttractor.X:
                    targetAlignmentAxis = targetTransform.right;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.right);
                    break;
                case AlignmentAxisAsAttractor.MinusX:
                    targetAlignmentAxis = -targetTransform.right;
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.right);
                    break;
                case AlignmentAxisAsAttractor.AnyX:
                    targetAlignmentAxis = Vector3.Project(sourceAlignementAxis, targetTransform.right);
                    targetAlignmentPlaneVector = Vector3.ProjectOnPlane(sourceAlignementPlaneAxis, targetTransform.right);
                    break;
            }

            if (useTargetPlaneAxisOrthogonalGuide)
            {
                var candidates = new Vector3[] { targetTransform.up, -targetTransform.up, targetTransform.right, -targetTransform.right, targetTransform.forward, -targetTransform.forward };
                var minAngle = float.PositiveInfinity;
                var bestCandidate = targetAlignmentPlaneVector;
                for (int i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    var angle = Vector3.Angle(targetAlignmentPlaneVector, candidate);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        bestCandidate = candidate;
                    }
                }
                targetAlignmentPlaneVector = bestCandidate;
            }

            var targetRotation = transform.rotation;
            switch (alignmentAxisAsAttracted)
            {
                case AlignmentAxisAsAttracted.Y:
                case AlignmentAxisAsAttracted.MinusY:
                    targetRotation = Quaternion.LookRotation(targetAlignmentPlaneVector, targetAlignmentAxis);
                    break;
                case AlignmentAxisAsAttracted.Z:
                case AlignmentAxisAsAttracted.MinusZ:
                    targetRotation = Quaternion.LookRotation(targetAlignmentAxis, targetAlignmentPlaneVector);
                    break;
                case AlignmentAxisAsAttracted.X:
                case AlignmentAxisAsAttracted.MinusX:
                    if (useTargetPlaneAxisOrthogonalGuide)
                    {            
                        // Handling X axis is more complex (and use cases are rare). Let's simply apply the same rotation
                        targetRotation = targetTransform.rotation;
                    }
                    else
                    {
                        // Handling X axis is more complex (and use cases are rare). Let's simply rotate to align the axis directly
                        targetRotation = transform.rotation * Quaternion.FromToRotation(sourceAlignementAxis, targetAlignmentAxis);
                    }                    
                    break;
            }

            return targetRotation;
        }

        void ApplyRotation(Quaternion targetRotation, float progress)
        {
            var localMagnetRotation = Quaternion.Inverse(rootNTRSP.transform.rotation) * transform.rotation;
            var rotation = targetRotation * Quaternion.Inverse(localMagnetRotation);

            if (progress < 1) rotation = Quaternion.Slerp(rootNTRSP.transform.rotation, rotation, progress);

            if (rb)
            {
                rb.rotation = rotation;
                rootNTRSP.transform.rotation = rotation;
            }
            else
            {
                rootNTRSP.transform.rotation = rotation;
            }
        }

        void ApplyPosition(Vector3 targetPosition, float progress)
        {
            var position = targetPosition - transform.position + rootNTRSP.transform.position;
            if (progress < 1) position = Vector3.Lerp(rootNTRSP.transform.position, position, progress);
            if (rb)
            {
                rb.position = position;
                rootNTRSP.transform.position = position;
            }
            else
            {
                rootNTRSP.transform.position = position;
            }
        }

        private void Update()
        {
            if (enableProximityDetectionWhileGrabbed && IsGrabbed && Object && Object.HasStateAuthority)
            {
                DetectProximityMagnet();
            }
        }

        void DetectProximityMagnet()
        {
            if (TryFindClosestMagnetInRange(out var remoteMagnet, out var distance))
            {
                if (remoteMagnet != proximityMagnet)
                {
                    if (proximityMagnet != null) {
                        if (onMagnetLeavingProximity != null) onMagnetLeavingProximity.Invoke(this, proximityMagnet);
                    }
                    proximityMagnet = remoteMagnet;
                    if (onMagnetDetectedInProximity != null) onMagnetDetectedInProximity.Invoke(this, remoteMagnet, distance);
                }
                if (onMagnetProximity != null) onMagnetProximity.Invoke(this, remoteMagnet, distance);
            }
            else if (proximityMagnet != null)
            {
                if (onMagnetLeavingProximity != null) onMagnetLeavingProximity.Invoke(this, proximityMagnet);
                proximityMagnet = null;
            }
        }
    }
}


