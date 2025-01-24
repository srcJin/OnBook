using UnityEngine;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;

namespace Fusion.Addons.HandsSync
{
    /**
     * In addition to parent HandRepresentationManager tasks, HardwareHandRepresentationManager handles:
     *  - handles which collider is used for grabbing based on hand mode
     */
    public class HardwareHandRepresentationManager : HandRepresentationManager
    {
        // Finger tracking component
        IBonesCollecter bonesCollector;

        HardwareHand hardwareHand;

        [Header("Grabbing Colliders")]
        [SerializeField]
        Collider controllerModeGrabCollider;
        [SerializeField]
        Collider fingerTrackingModeGrabCollider;

        bool controllerModeIndexTipColliderInitialState = true;
        bool fingerTrackingModeIndexTipColliderInitialState = true;

        [Header("Index tip Colliders")]
        [SerializeField]
        Collider controllerModeIndexTipCollider;
        [SerializeField]
        Collider fingerTrackingModeIndexTipCollider;

        public Transform IndexTip => (CurrentHandTrackingMode == HandTrackingMode.FingerTracking && fingerTrackingModeIndexTipCollider) ? fingerTrackingModeIndexTipCollider.transform : (controllerModeIndexTipCollider ? controllerModeIndexTipCollider.transform : null);

        #region HandRepresentationManager implementations
        public override HandTrackingMode CurrentHandTrackingMode => bonesCollector != null ? bonesCollector.CurrentHandTrackingMode : HandTrackingMode.NotTracked;

        public override RigPart Side => hardwareHand.side;
        public override GameObject FingerTrackingHandSkeletonParentGameObject => bonesCollector.gameObject;
        public override GameObject ControllerTrackingHandSkeletonParentGameObject => gameObject;
#endregion

        protected override void Awake()
        {
            base.Awake();
            if (bonesCollector == null) bonesCollector = GetComponentInChildren<IBonesCollecter>();
            if (hardwareHand == null) hardwareHand = GetComponentInChildren<HardwareHand>();
            if (fingerTrackingModeGrabCollider == null)
            {
                // Fallback attempt
                if (bonesCollector != null) fingerTrackingModeGrabCollider = bonesCollector.gameObject.GetComponentInChildren<Collider>();
            }
            if (controllerModeGrabCollider == null)
            {
                // Fallback attempt
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c != fingerTrackingModeGrabCollider) controllerModeGrabCollider = c;
            }

            if (fingerTrackingModeIndexTipCollider != null) fingerTrackingModeIndexTipColliderInitialState = fingerTrackingModeIndexTipCollider.enabled;
            if (controllerModeIndexTipCollider != null) controllerModeIndexTipColliderInitialState = controllerModeIndexTipCollider.enabled;

            if (fingerTrackingHandMeshRenderer == null && bonesCollector != null) fingerTrackingHandMeshRenderer = bonesCollector.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        protected override void Update()
        {
            base.Update();
            UpdateHandCollidersEnabled();
        }

        public void UpdateHandCollidersEnabled()
        {
            if (fingerTrackingModeGrabCollider && controllerModeGrabCollider)
            {
                if (fingerTrackingModeGrabCollider)
                    fingerTrackingModeGrabCollider.enabled = CurrentHandTrackingMode == HandTrackingMode.FingerTracking;

                if (controllerModeGrabCollider)
                    controllerModeGrabCollider.enabled = CurrentHandTrackingMode == HandTrackingMode.ControllerTracking;
            }
            if (fingerTrackingModeIndexTipCollider && controllerModeIndexTipCollider)
            {
                if (fingerTrackingModeIndexTipCollider)
                    fingerTrackingModeIndexTipCollider.enabled = CurrentHandTrackingMode == HandTrackingMode.FingerTracking && fingerTrackingModeIndexTipColliderInitialState;

                if (controllerModeIndexTipCollider)
                    controllerModeIndexTipCollider.enabled = CurrentHandTrackingMode == HandTrackingMode.ControllerTracking && controllerModeIndexTipColliderInitialState;
            }
        }
    }
}
