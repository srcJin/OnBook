using UnityEngine;

namespace Fusion.Addons.BlockingContact
{
    /***
     * 
     * BlockingSurface is used to define a surface that will block a BlockableTip
     * 
     ***/
    public class BlockingSurface : MonoBehaviour
    {
        [Tooltip("Threshold coordinate will be determined relatvely to this object.\n"
          +"Its scale should be Vector3.one for relative coordinates to be in meters (convenient for z values).\n"
          +"If not set, it will be the surface object itself (convenient for x/y values, that then will always range from -0.5 to 0.5)")]
        public Transform referential;
        public Vector3 positiveProximityThresholds = new Vector3(0.5f, 0.5f, 0.2f);
        public Vector3 negativeProximityThresholds = new Vector3(-0.5f, -0.5f, -0.001f);

        public float maxDepth = 0.005f;

        private void Awake()
        {
            if (referential == null) referential = transform;
        }
    }
}
