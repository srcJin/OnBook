using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.DataSyncHelpers
{
    /*
     * RingBufferLosslessSyncBehaviour and StreamSynchedBehaviour use their Object id has a key for identifying the component target of the streaming API requests by default
     * So if a same Object contains several of those components, collisions will occur. To avoid that, another mode can be used, using an hash, with small collisions risks, but more relevant in these cases
     * 
     * This component allow to switch to those modes for all object (to have a consistent behaviour)
     * 
     * Note that a risk of collision still exists. Ideally, override the TargetId() methods of RingBufferLosslessSyncBehaviour and StreamSynchedBehaviour to use real unique ids (stored in Network var for instance, to ensure unicity at initialisation)
     * This solution is a quick fix, that should work without additional effort for small scale scenario.
     * Note that it is not required if no NetworkObject contains more than 1 component among RingBufferLosslessSyncBehaviour and StreamSynchedBehaviour
     */
    [DefaultExecutionOrder(-10_000)]
    public class StreamingAPIConfiguration : MonoBehaviour
    {
        public static bool HandleMultipleStreamingAPIComponentCollisions = false;
        public bool handleMultipleStreamingAPIComponentCollisions = false;
        private void Awake()
        {
            HandleMultipleStreamingAPIComponentCollisions = handleMultipleStreamingAPIComponentCollisions;
        }
    }

}
