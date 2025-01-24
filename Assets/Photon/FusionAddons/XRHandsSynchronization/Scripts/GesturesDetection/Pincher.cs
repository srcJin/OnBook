using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared.Touch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.XRHandsSync

{
    public interface IPinchable
    {
        public void OnPincherContactStart(Pincher pincher, Pincher.PinchStatus pinchingStatus);
        public void OnPincherStay(Pincher pincher, Pincher.PinchStatus pinchingStatus);
        public void OnPincherContactEnd(Pincher pincher, Pincher.PinchStatus pinchingStatus);
    }

    public class Pincher : Toucher
    {
        [Header("Bounce prevention")]
        [SerializeField]
        float waitAfterConsomedPinchEnd = 0.2f;

        float nextAllowedPinchtime = -1;
        public struct PinchStatus {
            public float pinchingStart;
            public bool isPinching;
            public bool pinchConsumed;
        }
        PinchStatus pinchStatus;

        [Tooltip("If pinch can be used for grabbing, we will prevent ")]
        public bool preventConsumingPinchWhileGrabbing = true;
        public Grabber grabber;

        protected override void Awake()
        {
            base.Awake();
            if(grabber == null)
            {
                grabber = GetComponentInParent<Grabber>();
            }
        }

        public void OnHandPinching()
        {
            if (pinchStatus.isPinching == false)
            {
                if (nextAllowedPinchtime != -1)
                {
                    if (nextAllowedPinchtime > Time.time)
                    {
                        return;
                    }
                    nextAllowedPinchtime = -1;
                }
                pinchStatus.pinchingStart = Time.time;
                pinchStatus.pinchConsumed = false;
                pinchStatus.isPinching = true;
            }
        }

        public void OnHandNotPinching()
        {
            if (pinchStatus.isPinching)
            {
                //Debug.Log($"Stopped pinching {pinchStatus.pinchingStart} (consumed: {pinchStatus.pinchConsumed} / time: {Time.time})");
            }
            if (pinchStatus.isPinching && pinchStatus.pinchConsumed)
            {
                nextAllowedPinchtime = Time.time + waitAfterConsomedPinchEnd;
            }
            pinchStatus.isPinching = false;
        }

        public bool IsPinching => pinchStatus.isPinching;

        public bool TryConsumePinch()
        {
            if (IsPinching == false) return false;
            if (pinchStatus.pinchConsumed) return false;
            if (grabber && grabber.grabbedObject != null) return false;
            pinchStatus.pinchConsumed = true;
            return true;
        }

        IPinchable lastCheckedPinchable = null;
        IPinchable LookForPinchable(Collider other)
        {
            if (other != lastCheckCollider)
            {
                CheckCollider(other);
            }
            return lastCheckedPinchable;
        }

        protected override void CheckCollider(Collider other)
        {
            base.CheckCollider(other);
            if (lookForTouchableInColliderParent)
            {
                lastCheckedPinchable = other.GetComponentInParent<IPinchable>();
            }
            else
            {
                lastCheckedPinchable = other.GetComponent<IPinchable>();
            }
        }
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            IPinchable otherGameObjectPinchable = LookForPinchable(other);
            if (otherGameObjectPinchable != null)
            {
                otherGameObjectPinchable.OnPincherContactStart(this, pinchStatus);
            }
        }

        protected override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
            IPinchable otherGameObjectPinchable = LookForPinchable(other);
            if (otherGameObjectPinchable != null)
            {
                otherGameObjectPinchable.OnPincherStay(this, pinchStatus);
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            IPinchable otherGameObjectPinchable = LookForPinchable(other);
            if (otherGameObjectPinchable != null)
            {
                otherGameObjectPinchable.OnPincherContactEnd(this, pinchStatus);
            }
        }
    }
}
