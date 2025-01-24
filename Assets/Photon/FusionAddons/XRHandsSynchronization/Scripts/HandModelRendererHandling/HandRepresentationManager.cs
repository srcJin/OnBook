using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.HandsSync
{
    /**
    * 
    * HandRepresentationManager and its subclasses
    * - changes the hand's color or material based on the selected avatar skin
    * - handles which mesh is displayed based on hand mode
    **/
    public abstract class HandRepresentationManager : MonoBehaviour
    {
        public SkinnedMeshRenderer fingerTrackingHandMeshRenderer;
        public SkinnedMeshRenderer secondaryFingerTrackingHandMeshRenderer;
        public SkinnedMeshRenderer controllerTrackingHandMeshRenderer;

        [Header("Override material")]
        public Material overrideMaterialForRenderers;
        public enum MaterialOverrideMode
        {
            Override,
            InitialMaterial
        }
        public MaterialOverrideMode materialOverrideMode = MaterialOverrideMode.InitialMaterial;
        Material initialMaterial;

        Dictionary<Renderer, MaterialOverrideMode> currentMaterialStateByRenderer = new Dictionary<Renderer, MaterialOverrideMode>();

        [SerializeField]
        [Tooltip("Automatically set. For debugging purposes in the inspector")]
        HandTrackingMode currentHandTrackingMode;

        public abstract HandTrackingMode CurrentHandTrackingMode { get; }

        public abstract RigPart Side { get; }
        public abstract GameObject FingerTrackingHandSkeletonParentGameObject { get; }
        public abstract GameObject ControllerTrackingHandSkeletonParentGameObject { get; }

        [Header("Mode specific objects")]
        public List<GameObject> controllerModeOnlyGameObject = new List<GameObject>();
        public List<GameObject> fingerModeOnlyGameObject = new List<GameObject>();

        public void UpdateModeSpecificGameObjects()
        {
            foreach (var o in controllerModeOnlyGameObject)
            {
                if (o == null) continue;
                var shouldBeActive = CurrentHandTrackingMode == HandTrackingMode.ControllerTracking;
                if (o.activeSelf != shouldBeActive)
                {
                    o.SetActive(shouldBeActive);
                }
            }
            foreach (var o in fingerModeOnlyGameObject)
            {
                if (o == null) continue;
                var shouldBeActive = CurrentHandTrackingMode == HandTrackingMode.FingerTracking;
                if (o.activeSelf != shouldBeActive)
                {
                    o.SetActive(shouldBeActive);
                }
            }
        }

        public void UpdateHandMeshesEnabled()
        {
            if (fingerTrackingHandMeshRenderer)
            {
                fingerTrackingHandMeshRenderer.enabled = CurrentHandTrackingMode == HandTrackingMode.FingerTracking;
            }
            if (controllerTrackingHandMeshRenderer)
            {
                controllerTrackingHandMeshRenderer.enabled = CurrentHandTrackingMode == HandTrackingMode.ControllerTracking;
            }
            if (secondaryFingerTrackingHandMeshRenderer) secondaryFingerTrackingHandMeshRenderer.enabled = CurrentHandTrackingMode == HandTrackingMode.FingerTracking;
        }

        void FindRenderers()
        {
            if (fingerTrackingHandMeshRenderer == null)
            {
                fingerTrackingHandMeshRenderer = FingerTrackingHandSkeletonParentGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            if (controllerTrackingHandMeshRenderer == null)
            {
                foreach (var renderer in ControllerTrackingHandSkeletonParentGameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer == fingerTrackingHandMeshRenderer) continue;
                    if (renderer == secondaryFingerTrackingHandMeshRenderer) continue;
                    if (controllerTrackingHandMeshRenderer) Debug.LogError("Ambiguity in controllerTrackingHandMeshRenderer detection. Specify it manually " + name);
                    controllerTrackingHandMeshRenderer = renderer;
                }
            }
        }

        void UpdateRenderersMaterial()
        {
            ApplyOverrideMaterial(controllerTrackingHandMeshRenderer);
            ApplyOverrideMaterial(fingerTrackingHandMeshRenderer);
        }


        MaterialOverrideMode MaterialModeForRenderer(Renderer renderer)
        {
            if (currentMaterialStateByRenderer.ContainsKey(renderer)) return currentMaterialStateByRenderer[renderer];
            return MaterialOverrideMode.InitialMaterial;
        }

        public virtual void ApplyOverrideMaterial(Renderer renderer)
        {
            if (renderer == null) return;

            if (MaterialModeForRenderer(renderer) == materialOverrideMode) return;

            currentMaterialStateByRenderer[renderer] = materialOverrideMode;

            bool initialMaterialStillUsed = false;
            if (initialMaterial == null)
            {
                initialMaterialStillUsed = true;
                initialMaterial = renderer.material;
            }

            if (materialOverrideMode == MaterialOverrideMode.Override)
            {
                if (overrideMaterialForRenderers == null) Debug.LogError("Missing overrideMaterialForRenderers");
                ChangeRendererMaterial(renderer, overrideMaterialForRenderers);
                
            }
            else if (renderer.material == null || initialMaterialStillUsed == false)
            {
                ChangeRendererMaterial(renderer, initialMaterial);
           }
        }

        protected virtual void ChangeRendererMaterial(Renderer renderer, Material material)
        {
            if (renderer == null && material == null)
                return;

            renderer.material = material;
        }

        protected virtual void Awake()
        {
            // Set directly override material if renderers are set in the inspector
            ApplyOverrideMaterial(controllerTrackingHandMeshRenderer);
            ApplyOverrideMaterial(fingerTrackingHandMeshRenderer);
        }

        protected virtual void Update()
        {
            FindRenderers();
            UpdateRenderersMaterial();
            UpdateHandMeshesEnabled();
            UpdateModeSpecificGameObjects();
            currentHandTrackingMode = CurrentHandTrackingMode;
        }



        public virtual void ChangeHandColor(Color color)
        {
            FindRenderers();
            if (fingerTrackingHandMeshRenderer && MaterialModeForRenderer(fingerTrackingHandMeshRenderer) == MaterialOverrideMode.InitialMaterial) ChangeMaterialColor(fingerTrackingHandMeshRenderer.material, color);
            if (controllerTrackingHandMeshRenderer && MaterialModeForRenderer(controllerTrackingHandMeshRenderer) == MaterialOverrideMode.InitialMaterial) ChangeMaterialColor(controllerTrackingHandMeshRenderer.material, color);
            ChangeMaterialColor(initialMaterial, color);
        }

        public virtual void ChangeHandMaterial(Material material)
        {
            FindRenderers();
            if (MaterialModeForRenderer(fingerTrackingHandMeshRenderer) == MaterialOverrideMode.InitialMaterial) fingerTrackingHandMeshRenderer.sharedMaterial = material;
            if (MaterialModeForRenderer(controllerTrackingHandMeshRenderer) == MaterialOverrideMode.InitialMaterial) controllerTrackingHandMeshRenderer.sharedMaterial = material;
            initialMaterial = material;
        }

        public void ChangeMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            material.color = color;
            material.SetTexture("_BaseMap", null);
        }
    }
}
