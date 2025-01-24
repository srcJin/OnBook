using System.Collections.Generic;
using Fusion.XR.Shared.Grabbing;
using UnityEngine;


namespace Fusion.Addons.VisionOsHelpers
{

    /**
     * 
     * SpatialGrabber is used to simulate an hand and enable spatial grabbing.
     * 
     **/

    public class SpatialGrabber : Grabber
    {
        public string polyspatialIgnoredLayer = "PolySpatialIgnored";

        public bool isGrabbing = false;
        protected override bool IsGrabbing => isGrabbing;

        List<Collider> colliders = new List<Collider>();
        List<Renderer> renderers = new List<Renderer>();
        bool wasRequired = false;
        protected override void Awake()
        {
            foreach (var c in GetComponentsInChildren<Collider>()) if (c.enabled) colliders.Add(c);
            foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) renderers.Add(r);

            Required(false, force: true);

            int layer = LayerMask.NameToLayer(polyspatialIgnoredLayer);
            if (layer == -1)
            {
                Debug.LogError($"The layer '{polyspatialIgnoredLayer}' does not exists. Create it add remove it from the 'Collider object layer mask' in 'Project settings>Polyspatial'");
            }
            else
            {
                foreach (var collider in colliders)
                {
                    collider.gameObject.layer = layer;
                }
            }
        }

        // Required is used to enable/disable the spatialGrabber colliders & renderers 
        public void Required(bool isRequired, bool force = false)
        {
            if (force == false && isRequired == wasRequired) return;
            foreach (var c in colliders) c.enabled = isRequired;
            foreach (var r in renderers) r.enabled = isRequired;
            wasRequired = isRequired;
        }

        private void LateUpdate()
        {
            Required(isRequired: isGrabbing);
        }

        private void OnDisable()
        {
            if (grabbedObject) {
                Ungrab(grabbedObject);
            }
        }
    }
}
