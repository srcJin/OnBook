using System.Collections;
using System.Collections.Generic;
using Fusion.Addons.HandsSync;
using Fusion.Addons.XRHandsSync;
using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Fusion.Addons.VisionOsHelpers
{

    /**
    * 
    * VisionOSHandsConfiguration allows a quick automatic configuration of the hands for the Polyspatial vision OS platform.
    * 
    * The script : 
    *   - ensures that even if the hands are not detected for a short duration, the grabbing triggered by finger detection in FingerDrivenControllerInput still continues
    *   - uses LineMesh to display the beam used by a RayBeamer component
    *   - applies a specific layer to all collider in hands, that should be removed from Polyspatial handled colliders, to be sure that the grabbing/touching collider are not spatial touched by visionOS (which is probably not desired)
    *   - ensures that in case of no detection of the hands, the hand representation components do not try to fallback to controller, as there are no hand controller on visionOS
    * 
    **/
    public class VisionOSHandsConfiguration : MonoBehaviour
    {

        [Header("Configuration options")]
        [Tooltip("If true, ensures that even if the hands are not detected for a short duration, the grabbing triggered by finger detection in FingerDrivenControllerInput still continues")]
        public bool forceFingerTrackingActionPeristance = true;
        [Tooltip("If true, uses LineMesh to display the beam used by a RayBeamer component")]
        public bool useLineMeshForRayBeamers = true;
        public Material rayBeamerMaterial = null;
        [Tooltip("If true, applies a specific layer to all collider in hands, that should be removed from Polyspatial handled colliders, to be sure that the grabbing/touching collider are not spatial touched by visionOS (which is probably not desired)")]
        public bool ignoreHandColliderInPolyspatial = true;
        public string polyspatialIgnoredLayer = "PolySpatialIgnored";
        [Tooltip("If true, ensures that in case of no detection of the hands, the hand representation components do not try to fallback to controller, as there are no hand controller on visionOS")]
        public bool preventControllerTrackingFallback = true;
        [Tooltip("If true, use the provided (transparent probably) material for the network hands and hardware hands on visionOs, to hide them there")]
        public bool useOverideMaterialForHands = false;
        public Material handOverideMaterial;

        bool useOverideMaterialForHandsHardwareApplied = false;
        bool useOverideMaterialForHandsNetworkApplied = false;
        bool forceFingerTrackingActionPeristanceApplied = false;
        bool ignoreHandColliderInPolyspatialApplied = false;
        bool useLineMeshForRayBeamersApplied = false;
        bool preventControllerTrackingFallbackApplied = false;
        HardwareHand hardwareHand;

        private void Update()
        {
#if UNITY_VISIONOS
            if(hardwareHand == null) hardwareHand = GetComponentInParent<HardwareHand>();
            VisionOsConfiguration();
#endif
        }

        void VisionOsConfiguration()
        {
            // Finger grabbing
            if (forceFingerTrackingActionPeristance && forceFingerTrackingActionPeristanceApplied == false)
            {
                forceFingerTrackingActionPeristanceApplied = true;
                var fingerDriverControllerInput = GetComponentInChildren<FingerDrivenControllerInput>();
                if (fingerDriverControllerInput)
                {
                    // On vision OS, there is no need to fallback to controller actions (we don't have any controllers)
                    // Besides, during fast movement, hand tracking can be lost, and object would be ungrabbed upon loosing control
                    fingerDriverControllerInput.alwaysKeepHandCommandControl = true;
                }
            }

            // Hand colliders
            if (ignoreHandColliderInPolyspatial && ignoreHandColliderInPolyspatialApplied == false)
            {
                ignoreHandColliderInPolyspatialApplied = true;
                int layer = LayerMask.NameToLayer(polyspatialIgnoredLayer);
                if (layer == -1)
                {
                    Debug.LogError($"The layer '{polyspatialIgnoredLayer}' does not exists. Create it add remove it from the 'Collider object layer mask' in 'Project settings>Polyspatial'");
                }
                else
                {
                    foreach(var collider in GetComponentsInChildren<Collider>())
                    {
                        collider.gameObject.layer = layer;
                    }
                }
            }

            // Ray beamer
            // Line renderers are not yet available on polyspatial: placing a LineRendererToLineMesh to replace them
            if (useLineMeshForRayBeamers && useLineMeshForRayBeamersApplied == false) {
                useLineMeshForRayBeamersApplied = true;
                var beamer = GetComponentInChildren<RayBeamer>();
                if (beamer)
                {
                    var lineRendererObject = beamer.gameObject;
                    if (beamer.lineRenderer) lineRendererObject = beamer.lineRenderer.gameObject;
                    var meshRenderer = lineRendererObject.AddComponent<MeshRenderer>();
                    if (rayBeamerMaterial)
                    {
                        meshRenderer.material = rayBeamerMaterial;
                    }
                    else
                    {
                        meshRenderer.material = Resources.Load<Material>("LineSGMaterial");
                    }
                    lineRendererObject.AddComponent<MeshFilter>();
                    var lineRendererToLineMesh = lineRendererObject.AddComponent<LineRendererToLineMesh>();
                    lineRendererToLineMesh.checkPositionsEveryFrame = true;
                    lineRendererToLineMesh.replicateLineRendererEnabledStatus = true;
                }
            }

            // Hand representation

            // vision OS do not have controllers. So if the hand stop being detected, we should not switch back to controller mode by default
            if (preventControllerTrackingFallback && preventControllerTrackingFallbackApplied == false) {
                preventControllerTrackingFallbackApplied = true;
                XRHandCollectableSkeletonDriver xrHandCollectableSkeletonDriver = GetComponentInChildren<XRHandCollectableSkeletonDriver>();
                if (xrHandCollectableSkeletonDriver)
                {
                    xrHandCollectableSkeletonDriver.controllerTrackingMode = XRHandCollectableSkeletonDriver.ControllerTrackingMode.NeverAvailable;
                }
            }

            if (useOverideMaterialForHands) {
                if (useOverideMaterialForHandsHardwareApplied == false)
                {
                    useOverideMaterialForHandsHardwareApplied = true;
                    foreach (var hardwareHandRepresentationManager in GetComponentsInChildren<HardwareHandRepresentationManager>())
                    {
                        hardwareHandRepresentationManager.materialOverrideMode = HandRepresentationManager.MaterialOverrideMode.Override;
                        if (handOverideMaterial)
                        {
                            hardwareHandRepresentationManager.overrideMaterialForRenderers = handOverideMaterial;
                        }
                    }
                }
                if (useOverideMaterialForHandsNetworkApplied == false)
                {
                    foreach (var networkHandRepresentationManager in FindObjectsOfType<NetworkHandRepresentationManager>())
                    {
                        if (networkHandRepresentationManager.networkHand.IsLocalNetworkRig == false || networkHandRepresentationManager.networkHand.side != hardwareHand.side) {
                            continue;
                        }
                        useOverideMaterialForHandsNetworkApplied = true;
                        networkHandRepresentationManager.materialOverrideMode = HandRepresentationManager.MaterialOverrideMode.Override;
                        if (handOverideMaterial)
                        {
                            networkHandRepresentationManager.overrideMaterialForRenderers = handOverideMaterial;
                        }
                    }
                }
            }
        }

    }

}
