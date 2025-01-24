using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XRShared.GrabbableMagnet
{
    /**
     * StaticMagnet components will atttract IMagnets to them, but won't be attracted themselves by other magnets
     */
    public class StaticMagnet : MonoBehaviour, IMagnet
    {
        [Header("Attractor options")]
        public AlignmentAxisAsAttractor alignmentAxisAsAttractor = AlignmentAxisAsAttractor.MinusY;
        [Tooltip("AttracktOnlyOnAlignmentAxis: The attracted magnet will only move to project itself on the plane defined by the attractor magnet alignment axis" +
            "\nMatchAttracktingMagnetPosition : the attracted magnet will match the attractork magnet position")]
        public AttractedMagnetMove attractedMagnetMove = AttractedMagnetMove.MatchAttracktingMagnetPosition;
        [Tooltip("MatchAlignmentAxis: The attracted object will rotate only to align the attracted axis and the attractor axis\nMatchAlignmentAxisWithOrthogonalRotation: The attracted object will also rotate to only have 90 angles between other axis")]
        public AttractedMagnetRotation attractedMagnetRotation = AttractedMagnetRotation.MatchAlignmentAxisWithOrthogonalRotation;

        public Vector3 localOffset = Vector3.zero;
        public bool ignoreOffsetSign = true;

        [Header("Automatic layer setup")]
        public string magnetLayer = "Magnets";
        public bool applyLayerToChildren = true;

        private void Awake()
        {
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
                    if (applyLayerToChildren) {
                        foreach (var collider in GetComponentsInChildren<Collider>())
                        {
                            collider.gameObject.layer = layer;
                        }
                    }
                }
            }               
        }

        #region IMagnet
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

        public Vector3 SnapTargetPosition(Vector3 position)
        {
            // Ignore scale
            var trsMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            var offsetetPosition = trsMatrix.MultiplyPoint3x4(localOffset);
            var offset = offsetetPosition - transform.position;
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
                var projection = projectionPlane.ClosestPointOnPlane(position);
                var offsetedProjection = projection + offset;
                if (ignoreOffsetSign)
                {
                    var reverseOffsetetPosition = trsMatrix.MultiplyPoint3x4(-localOffset);
                    var reverseOffset = reverseOffsetetPosition - transform.position;
                    var reverseOffsetedProjection = projectionPlane.ClosestPointOnPlane(position) + reverseOffset;
                    var reverseDistance = Vector3.Distance(position, reverseOffsetedProjection);
                    var distance = Vector3.Distance(position, offsetedProjection);
                    if (reverseDistance < distance)
                    {
                        offsetedProjection = reverseOffsetedProjection;
                    }
                }
                return offsetedProjection;
            }
            else
            {
                return transform.position + offset;
            }
        }
        #endregion
    }
}
