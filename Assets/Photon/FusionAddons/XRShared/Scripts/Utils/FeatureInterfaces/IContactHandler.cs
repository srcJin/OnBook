using UnityEngine;

namespace Fusion.XR.Shared
{
    // Interface to warn other components that this one is already handling virtual contact (to avoid triggering effects, like a pen detecting real life contact - with pressure sensor, and virtual surface contact at the same time - with colliders)
    public interface IContactHandler
    {
        public bool IsHandlingContact { get; }
    }
}
