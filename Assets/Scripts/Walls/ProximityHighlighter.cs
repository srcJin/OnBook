using Fusion.XRShared.GrabbableMagnet;
using UnityEngine;

/***
 * 
 * The ProximityHighlighter class implements the IProximityTarget interface. 
 * It is used to enable a mesh on a MagneticARPlane only when in proximity to a magnet. 
 * It includes methods for detecting and handling proximity to a magnet (MagnetPoint).
 * 
 ***/

public interface IProximityTarget {
    public void ProximityStart(ProximityHighlighter proximityHighlighter, float startDistance);
    public void ProximityChange(ProximityHighlighter proximityHighlighter, float startDistance, float distance);
    public void ProximityEnd(ProximityHighlighter proximityHighlighter);
}

public class ProximityHighlighter : MonoBehaviour
{
    MagnetPoint magnet;

    IProximityTarget proximityTarget;
    float startDistance;

    void Awake()
    {
        magnet = GetComponentInChildren<MagnetPoint>();
        magnet.enableProximityDetectionWhileGrabbed = true;
        magnet.onMagnetDetectedInProximity.AddListener(OnMagnetDetectedInProximity);
        magnet.onMagnetLeavingProximity.AddListener(OnMagnetLeavingProximity);
        magnet.onMagnetProximity.AddListener(OnMagnetProximity);
        magnet.onSnapToMagnet.AddListener(OnSnapToMagnet);
    }


    private void OnSnapToMagnet()
    {
        if (proximityTarget != null)
        {
            proximityTarget.ProximityEnd(this);
        }
    }

    private void OnMagnetLeavingProximity(IMagnet arg0, IMagnet attractor)
    {
        if (attractor is MonoBehaviour attractorBehaviour && attractorBehaviour.TryGetComponent<IProximityTarget>(out var target))
        {
            target.ProximityEnd(this);
            proximityTarget = null;
        }
    }

    private void OnMagnetDetectedInProximity(IMagnet arg0, IMagnet attractor, float distance)
    {
        if (attractor is MonoBehaviour attractorBehaviour && attractorBehaviour.TryGetComponent<IProximityTarget>(out var target))
        {
            startDistance = distance;
            proximityTarget = target;
            proximityTarget.ProximityStart(this, startDistance);
            proximityTarget.ProximityChange(this, startDistance, distance);
        }
    }

    private void OnMagnetProximity(IMagnet arg0, IMagnet attractor, float distance)
    {
        if (proximityTarget != null)
        {
            proximityTarget.ProximityChange(this, startDistance, distance);
        }
    }

}
