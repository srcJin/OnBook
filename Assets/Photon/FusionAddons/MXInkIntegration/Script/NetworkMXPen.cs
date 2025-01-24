using Fusion.Addons.LineDrawing;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.MXPenIntegration {

    [DefaultExecutionOrder(NetworkMXPen.EXECUTION_ORDER)]
    public class NetworkMXPen : NetworkBehaviour
    {
        public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 1;

        [Networked, OnChangedRender(nameof(OnChangeIsStylusActive))]
        public NetworkBool IsStylusActive { get; set; }

        [Networked]
        public NetworkBool IsReplacingRightHand { get; set; }

#if OCULUS_SDK_AVAILABLE
        protected StylusHandler localHardwareStylus;

        public StylusHandler LocalHardwareStylus => localHardwareStylus;
#endif

        [SerializeField] bool automaticallyDetectNetworkHands = true;

        public List<GameObject> penModeGameObjects = new List<GameObject>();
        public List<GameObject> noPenReplacingRightHandModeGameObjects = new List<GameObject>();
        public List<GameObject> noPenReplacingLeftHandModeGameObjects = new List<GameObject>();

        protected NetworkLineDrawer networkLineDrawer;

        IContactHandler[] contactHandlers;

        [Tooltip("If true, if any component implementing IContactHandler returns true for IsHandlingContact, the tip pressure drawing will be ignored")]
        [SerializeField] bool ignoreContactPressureIfVirtualContactAlreadyHandled = true;

        protected IFeedbackHandler feedback;
        [Header("Drawing Feedback")]
        [SerializeField] string audioType;
        protected virtual void Awake()
        {
            contactHandlers = GetComponentsInChildren<IContactHandler>();
            networkLineDrawer = GetComponentInChildren<NetworkLineDrawer>();
            feedback = GetComponent<IFeedbackHandler>();
            if (automaticallyDetectNetworkHands)
            {
                var rig = GetComponentInParent<NetworkRig>();
                if (rig)
                {
                    if (noPenReplacingRightHandModeGameObjects.Contains(rig.rightHand.gameObject) == false)
                    {
                        noPenReplacingRightHandModeGameObjects.Add(rig.rightHand.gameObject);
                    }
                    if (noPenReplacingLeftHandModeGameObjects.Contains(rig.leftHand.gameObject) == false)
                    {
                        noPenReplacingLeftHandModeGameObjects.Add(rig.leftHand.gameObject);
                    }
                }
            }
        }

        public override void Spawned()
        {
            base.Spawned();
#if OCULUS_SDK_AVAILABLE
            if (Object.HasStateAuthority)
            {
                localHardwareStylus = FindObjectOfType<VrStylusHandler>();
            }
#endif
            UpdateDisplayedGameObjects();
        }

#if OCULUS_SDK_AVAILABLE
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (localHardwareStylus)
            {
                transform.position = localHardwareStylus.transform.position;
                transform.rotation = localHardwareStylus.transform.rotation;
                IsStylusActive = localHardwareStylus.CurrentState.isActive;
                IsReplacingRightHand = localHardwareStylus.CurrentState.isOnRightHand;
            }
        }

        public override void Render()
        {
            base.Render();
         
            // Extrapolation
            if (localHardwareStylus)
            {
                transform.position = localHardwareStylus.transform.position;
                transform.rotation = localHardwareStylus.transform.rotation;

                if (networkLineDrawer)
                {
                    VolumeDrawing();
                }
            }
        }

        protected virtual void VolumeDrawing()
        {
            var pressure = localHardwareStylus.CurrentState.cluster_middle_value;
            bool shouldIgnoreContactPressure = false;
            if (ignoreContactPressureIfVirtualContactAlreadyHandled)
            {
                foreach (var handler in contactHandlers)
                {
                    if (handler.IsHandlingContact)
                    {
                        shouldIgnoreContactPressure = true;
                        break;
                    }
                }
            }
            if (shouldIgnoreContactPressure == false)
            {
                var tipPressure = localHardwareStylus.CurrentState.tip_value;
                pressure = Mathf.Max(pressure, tipPressure);
            }
            if (pressure > 0.01f)
            {
                networkLineDrawer.AddPoint(pressure: pressure);
                if (feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
                {
                    feedback.PlayAudioAndHapticFeeback(audioType: audioType, audioOverwrite: false, hapticAmplitude: pressure);
                }
            }
            else if (networkLineDrawer.IsDrawingLine)
            {
                networkLineDrawer.StopLine();
               
                if (feedback != null)
                {
                    feedback.StopAudioFeeback();
                }
            }

            // Stop drawing causes
            bool shouldStopCurrentDrawing = ShouldStopCurrentVolumeDrawing();
            if (pressure == 0 && shouldStopCurrentDrawing)
            {
                networkLineDrawer.StopDrawing();
                if (feedback != null)
                {
                    feedback.StopAudioFeeback();
                }
            }
            if (networkLineDrawer.IsDrawing && IsStylusActive == false)
            {
                networkLineDrawer.StopDrawing();
                if (feedback != null)
                {
                    feedback.StopAudioFeeback();
                }
            }
        }

        protected virtual bool ShouldStopCurrentVolumeDrawing()
        {
            return localHardwareStylus.CurrentState.cluster_back_value || localHardwareStylus.CurrentState.cluster_front_value;
        }
#endif
        protected virtual void OnChangeIsStylusActive()
        {
            Debug.Log("OnChangeIsStylusActive");
            UpdateDisplayedGameObjects();
        }

        void UpdateDisplayedGameObjects()
        {
            foreach (var o in penModeGameObjects)
            {
                if (o == null) continue;
                if (o.activeSelf != IsStylusActive)
                {
                    o.SetActive(IsStylusActive);
                }
            }

            foreach (var o in noPenReplacingRightHandModeGameObjects)
            {
                if (o == null) continue;
                var active = IsStylusActive && IsReplacingRightHand;
                if (o.activeSelf != !active)
                {
                    o.SetActive(!active);
                }
            }

            foreach (var o in noPenReplacingLeftHandModeGameObjects)
            {
                if (o == null) continue;
                var active = IsStylusActive && !IsReplacingRightHand;
                if (o.activeSelf != !active)
                {
                    o.SetActive(!active);
                }
            }
        }
    }
}

