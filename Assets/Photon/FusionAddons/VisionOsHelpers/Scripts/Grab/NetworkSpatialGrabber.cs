using Fusion.XR.Shared.Grabbing;

namespace Fusion.Addons.VisionOsHelpers
{
    /**
     * 
     * Networked version of the Spatial Grabber.
     * It follows the SpatialGrabber position & rotation
     *      
     **/

    public class NetworkSpatialGrabber : NetworkGrabber
    {
        public Grabber grabber;
        public override void Spawned()
        {
            base.Spawned();
            if (Object.HasStateAuthority)
            {
                var grabbers = FindObjectsOfType<SpatialGrabber>();
                foreach (var g in grabbers)
                {
                    if (g.networkGrabber == null)
                    {
                        grabber = g;
                        grabber.networkGrabber = this;
                        break;
                    }
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            FollowHardwareGrabber();
        }

        public override void Render()
        {
            base.Render();
            FollowHardwareGrabber();
        }

        void FollowHardwareGrabber()
        {
            if (Object.HasStateAuthority && grabber)
            {
                transform.position = grabber.transform.position;
                transform.rotation = grabber.transform.rotation;
            }
        }
    }
}
 

