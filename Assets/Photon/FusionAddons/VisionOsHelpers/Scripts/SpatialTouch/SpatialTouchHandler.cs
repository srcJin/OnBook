using System;
using System.Collections.Generic;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
#if POLYSPATIAL_SDK_AVAILABLE
using Unity.PolySpatial;
using Unity.PolySpatial.InputDevices;
#endif
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Fusion.Addons.VisionOsHelpers
{
    public interface ISpatialTouchListener
    {
#if POLYSPATIAL_SDK_AVAILABLE

        void TouchStart(SpatialPointerKind interactionKind, Vector3 interactionPosition, SpatialPointerState primaryTouchData);
        void TouchEnd();
        void TouchStay(SpatialPointerKind interactionKind, Vector3 interactionPosition, SpatialPointerState primaryTouchData);
#endif
    }

    /**
    * 
    * SpatialTouchHandler class detects user's interactions (touch, pinch, indirect pinch) thanks to Unity Polyspatial.
    * Touch:
    * It raises TouchStart, TouchEnd & TouchStay events for ISpatialTouchListener.
    * Grabbing: 
    * The SpatialTouchTracker struct can keep track of up to 2 spatial touch event at the same time.
    * If present in the scene, up to 2 SpatialGrabber will be associated to the SpatialTouchTracker struct to handle spatial grabbing
    * In unbounded mode, if replaceContactGrabber is set to false, spatial touch for grabbing is not taken into account to avoid duplicate logic,
    *  as, grabbing while touching is already handled by the normal grabber logic. 
    * If replaceContactGrabber is set to true (default, as hand tracking is refreshed less often than spatial touches on visionOS), normal Grabber components on hands will be disabled, to avoid this duplicate logic
    *  
    **/

    public class SpatialTouchHandler : MonoBehaviour
    {
#if POLYSPATIAL_SDK_AVAILABLE
        [System.Serializable]
        public struct SpatialTouchTracker
        {
            [Header("Debug")]
            public GameObject debugRepresentation;
            public TMPro.TMP_Text debugText;
            public Vector3 debugTextOffset;
            public Vector3 debugTextRotationOffset;


            [Header("Associated SpatialGrabber")]
            [Tooltip("Automatically detected in the scene at start")]
            public SpatialGrabber grabber;

            [Header("Grabbing and touch info")]
            public GameObject previousObject;
            public GameObject lastSpatialTouchedObject;
            public SpatialPointerKind previousKind;
            public List<ISpatialTouchListener> lastSpatialTouchedListeners;
            // Used for a touch during the current update
            public bool isUsed;


            public void OnTouchUpdate(SpatialPointerState primaryTouchData, VolumeCamera.PolySpatialVolumeCameraMode currentMode, bool doNotUseContactSpatialGrabbingInUnboundedMode, bool preventGrabbingSpatialTouchListeners)
            {
                SpatialPointerKind interactionKind = primaryTouchData.Kind;
                GameObject objectBeingInteractedWith = primaryTouchData.targetObject;
                Vector3 interactionPosition = primaryTouchData.interactionPosition;

                if (previousObject != objectBeingInteractedWith || interactionKind != previousKind)
                {
                    previousObject = objectBeingInteractedWith;
                    previousKind = interactionKind;
                }

                if (objectBeingInteractedWith != lastSpatialTouchedObject)
                {
                    // TouchEnd callback in case of change on the interacted object (if it has spatial touch listeners)
                    if (lastSpatialTouchedObject != null)
                    {
                        foreach (var listener in lastSpatialTouchedListeners)
                        {
                            listener.TouchEnd();
                        }
                    }
                    lastSpatialTouchedObject = objectBeingInteractedWith;
                    lastSpatialTouchedListeners = new List<ISpatialTouchListener>(objectBeingInteractedWith.GetComponentsInParent<ISpatialTouchListener>());
                    foreach (var listener in lastSpatialTouchedListeners)
                    {
                        listener.TouchStart(interactionKind, interactionPosition, primaryTouchData);
                    }
                }
                else
                {
                    // TouchStay callback in case of keeping the same interacted object (if it has spatial touch listeners)
                    foreach (var listener in lastSpatialTouchedListeners)
                    {
                        listener.TouchStay(interactionKind, interactionPosition, primaryTouchData);
                    }
                }

                if ((preventGrabbingSpatialTouchListeners == false || lastSpatialTouchedListeners.Count == 0) && grabber)
                {
                    if (interactionKind == SpatialPointerKind.Touch)
                    {
                        // No grab while just touching
                        grabber.isGrabbing = false;
                    }
                    else
                    {
                        // Grabbing for direct
                        bool contactGrabbing = interactionKind == SpatialPointerKind.Touch || interactionKind == SpatialPointerKind.DirectPinch;
                        // In unbounded mode, grabbing while touching is already handled by normal grabber logic: skip spatial touch for grabbing to avoid duplicate logic
                        if (doNotUseContactSpatialGrabbingInUnboundedMode && currentMode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded && contactGrabbing)
                        {
                            grabber.isGrabbing = false;
                            grabber.transform.position = interactionPosition;
                            grabber.transform.rotation = primaryTouchData.inputDeviceRotation;
                        }
                        else
                        {
                            // Grabbing is possible, due to either:
                            // - indirect pinch
                            // - in bounded mode
                            // - or direct pinch with doNotUseContactSpatialGrabbingInUnboundedMode == false
                            if (grabber.gameObject.activeSelf == false)
                            {
                                grabber.gameObject.SetActive(true);
                            }
                            grabber.isGrabbing = true;
                            grabber.transform.position = interactionPosition;
                            grabber.transform.rotation = primaryTouchData.inputDeviceRotation;
                        }

                    }
                }

                DebugPositioning(primaryTouchData);
            }

            public void OnTouchInactive()
            {
                if (grabber)
                {
                    grabber.isGrabbing = false;
                }
                DisableDebug();
                if (lastSpatialTouchedObject != null)
                {
                    foreach (var listener in lastSpatialTouchedListeners)
                    {
                        listener.TouchEnd();
                    }
                }
                lastSpatialTouchedObject = null;
                lastSpatialTouchedListeners.Clear();
            }

            public void DebugPositioning(SpatialPointerState primaryTouchData)
            {
                if (debugRepresentation)
                {
                    debugRepresentation.SetActive(true);
                    debugRepresentation.transform.position = primaryTouchData.inputDevicePosition;
                    debugRepresentation.transform.rotation = primaryTouchData.inputDeviceRotation;
                }
                if (debugText)
                {
                    debugText.gameObject.SetActive(true);
                    debugText.text = primaryTouchData.targetObject.name;
                    debugText.transform.position = primaryTouchData.inputDevicePosition + debugTextOffset;
                    debugText.transform.rotation = primaryTouchData.inputDeviceRotation * Quaternion.Euler(debugTextRotationOffset);
                }
            }

            public void DisableDebug()
            {
                if (debugRepresentation)
                {
                    debugRepresentation.SetActive(false);
                }
                if (debugText)
                {
                    debugText.gameObject.SetActive(false);
                }
            }
        }

        public VolumeCamera volumeCamera;
        const int MAX_TOUCHES = 2;
        public SpatialTouchTracker[] trackers = new SpatialTouchTracker[MAX_TOUCHES];
        [Header("Interaction configuration")]
        [Tooltip("If true, regular Grabber component on the HardwareRig will be disabled on visionOS")]
        public bool replaceContactGrabber = true;
        [Tooltip("If true, regular Toucher component on the HardwareRig will be disabled on visionOS")]
        public bool replaceTouchers = false;
        [Tooltip("If true, spatial touch interaction won't trigger grabbing on object having components implementing ISpatialTouchListener")]
        public bool preventGrabbingSpatialTouchListeners = true;

        VolumeCamera.PolySpatialVolumeCameraMode currentMode;
        bool doNotUseContactSpatialGrabbingInUnboundedMode = false;
        bool hardwareRigGrabberDisabled = false;
        bool hardwareRigTouchersDisabled = false;


        private void Awake()
        {
            var grabbers = FindObjectsOfType<SpatialGrabber>();
            for (int i = 0; i < trackers.Length; i++)
            {
                if (grabbers.Length > i) trackers[i].grabber = grabbers[i];
                trackers[i].lastSpatialTouchedListeners = new List<ISpatialTouchListener>();
            }
            volumeCamera = FindObjectOfType<VolumeCamera>(true);
            if (volumeCamera)
            {
                volumeCamera.OnWindowEvent.AddListener(OnVolumeCameraWindowEvent);
            }
        }

        private void OnVolumeCameraWindowEvent(VolumeCamera.WindowState windowState)
        {
            currentMode = windowState.Mode;
            Debug.Log("OnVolumeCameraWindowEvent: VolumeCameraMode: " + currentMode);
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Update()
        {
            doNotUseContactSpatialGrabbingInUnboundedMode = !replaceContactGrabber;
#if UNITY_VISIONOS && !UNITY_EDITOR
            if (replaceTouchers && hardwareRigTouchersDisabled == false)
            {
                var hardwareRig = FindObjectOfType<HardwareRig>();
                if (hardwareRig)
                {
                    var hardwareRigTouchers = hardwareRig.GetComponentsInChildren<XR.Shared.Touch.Toucher>();
                    foreach (var t in hardwareRigTouchers) t.enabled = false;
                    hardwareRigTouchersDisabled = true;
                }
            }
            if (doNotUseContactSpatialGrabbingInUnboundedMode == false && hardwareRigGrabberDisabled == false)
            {
                var hardwareRig = FindObjectOfType<HardwareRig>();
                if (hardwareRig)
                {
                    var hardwareRigGrabbers = hardwareRig.GetComponentsInChildren<Grabber>();
                    foreach (var g in hardwareRigGrabbers) g.enabled = false;
                    hardwareRigGrabberDisabled = true;
                }
            }

            for (int i = 0; i < trackers.Length; i++) trackers[i].isUsed = false;
            var activeTouches = Touch.activeTouches;
            // You can determine the number of active inputs by checking the count of activeTouches
            foreach (var activeTouch in activeTouches)
            {
                // For getting access to PolySpatial (visionOS) specific data you can pass an active touch into the EnhancedSpatialPointerSupport()
                SpatialPointerState primaryTouchData = EnhancedSpatialPointerSupport.GetPointerState(activeTouch);

                GameObject objectBeingInteractedWith = primaryTouchData.targetObject;

                int freeTrackerIndex = -1;
                bool touchHandled = false;
                for (int i = 0; i < trackers.Length; i++)
                {
                    if (trackers[i].isUsed == true)
                    {
                        // Multitouch on the same object not handled
                        continue;
                    }
                    else if (trackers[i].lastSpatialTouchedObject == objectBeingInteractedWith)
                    {
                        trackers[i].OnTouchUpdate(primaryTouchData, currentMode, doNotUseContactSpatialGrabbingInUnboundedMode, preventGrabbingSpatialTouchListeners);
                        trackers[i].isUsed = true;
                        touchHandled = true;
                        break;
                    }
                    else if (freeTrackerIndex == -1 && trackers[i].lastSpatialTouchedObject == null)
                    {
                        freeTrackerIndex = i;
                    }
                }
                if (freeTrackerIndex != -1)
                {
                    if(touchHandled == false)
                    {
                        trackers[freeTrackerIndex].OnTouchUpdate(primaryTouchData, currentMode, doNotUseContactSpatialGrabbingInUnboundedMode, preventGrabbingSpatialTouchListeners);
                        trackers[freeTrackerIndex].isUsed = true;
                    }
                }

            }
            for (int i = 0; i < trackers.Length; i++)
            {
                if (trackers[i].isUsed == false)
                {
                    trackers[i].OnTouchInactive();
                }
            }
#endif
        }
#endif
    }
}