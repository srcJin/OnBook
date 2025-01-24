using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;
using System;

namespace Fusion.XRShared.GrabbableMagnet
{
    public enum AlignmentAxisAsAttracted { 
        X, Y, Z, 
        MinusX, MinusY, MinusZ 
    }
    public enum AlignmentAxisAsAttractor { 
        X, Y, Z, 
        MinusX, MinusY, MinusZ, 
        AnyX, AnyY, AnyZ 
    }

    public enum AttractedMagnetMove {
        // The attracted magnet will only move to project itself on the plane defined by the attractor magnet alignment axis
        AttracktOnlyOnAlignmentAxis,
        // the attracted magnet will match the attractork magnet position
        MatchAttracktingMagnetPosition
    };

    public enum AttractedMagnetRotation { 
        // The attracted object will rotate only to align the attracted axis and the attractor axis
        MatchAlignmentAxis,
        // The attracted object will also rotate to only have 90 angles between other axis
        MatchAlignmentAxisWithOrthogonalRotation
    };

    public interface IMagnet
    {
        public Vector3 SnapTargetPosition(Vector3 position);
        public AlignmentAxisAsAttractor AlignmentAxisAsAttractor { get; }
        public AttractedMagnetMove AttractedMagnetMove { get; set; }
        public AttractedMagnetRotation AttractedMagnetRotation { get; set; }

#pragma warning disable IDE1006 // Naming Styles
        public Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles
    }

    public interface IAttracktableMagnet : IMagnet {
        public AlignmentAxisAsAttracted AlignmentAxisAsAttracted { get; }
        public bool CheckOnUngrab { get; set; }
        public float MagnetRadius { get; set; }
        public bool TryFindClosestMagnetInRange(out IMagnet closestMagnet, out float distance);
        public void SnapToMagnet(IMagnet magnet);
        public MagnetCoordinator MagnetCoordinator { get; }
    }

    [DefaultExecutionOrder(MagnetPoint.EXECUTION_ORDER)]
    public class MagnetCoordinator : NetworkBehaviour
    {
        NetworkGrabbable networkGrabbable;
        public bool overrideMagnetRadius = true;
        public float magnetRadius = 0.1f;

        List<IAttracktableMagnet> magnets = new List<IAttracktableMagnet>();
        private void Awake()
        {
            networkGrabbable = GetComponentInParent<NetworkGrabbable>();
            networkGrabbable.onDidUngrab.AddListener(OnDidUngrab);
            magnets = new List<IAttracktableMagnet>(GetComponentsInChildren<IAttracktableMagnet>());
            foreach (var magnet in magnets)
            {
                magnet.CheckOnUngrab = false;
            }
        }

        private void OnDidUngrab()
        {
            if (overrideMagnetRadius)
            {
                foreach (var magnet in magnets)
                {
                    magnet.MagnetRadius = magnetRadius;
                }
            }
            CheckMagnetProximity();
        }

        [ContextMenu("CheckMagnetProximity")]
        public void CheckMagnetProximity()
        {
            if (Object && Object.HasStateAuthority && networkGrabbable.IsGrabbed == false)
            {
                float minDistance = float.PositiveInfinity;
                IAttracktableMagnet closestLocalMagnet = null;
                IMagnet closestRemoteMagnet = null;
                foreach (var magnet in magnets)
                {
                    if (magnet.TryFindClosestMagnetInRange(out var remoteMagnet, out var distance))
                    {
                        if (distance < minDistance)
                        {
                            closestLocalMagnet = magnet;
                            closestRemoteMagnet = remoteMagnet;
                            minDistance = distance;
                        }
                    }
                }
                if (closestLocalMagnet != null)
                {
                    closestLocalMagnet.SnapToMagnet(closestRemoteMagnet);
                }
            }
        }
    }

}
