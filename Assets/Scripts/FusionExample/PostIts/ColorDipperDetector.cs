using Fusion;
using Fusion.XR.Shared;
using UnityEngine;

/***
 * 
 * The ColorDipperDetector class is responsible for detecting when a ColorDipper object enters its trigger zone.
 * Then, it ensures that only the state authority can trigger the color change request to the IColorProvider component. 
 * 
 ***/
public class ColorDipperDetector : NetworkBehaviour
{
    IColorProvider colorProvider;
    bool colorChangeRequest = false;
    Color requestedColor;

    private void Awake()
    {
        colorProvider = GetComponent<IColorProvider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object && Object.HasStateAuthority)
        {
            ColorDipper colorDipper = other.GetComponentInParent<ColorDipper>();
            if (colorDipper != null)
            {
                colorChangeRequest = true;
                requestedColor = colorDipper.color;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if(colorChangeRequest)
        {
            colorProvider.CurrentColor = requestedColor;
            colorChangeRequest = false;
        }
    }
}
