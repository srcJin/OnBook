using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class FingerDrivenHardwareRigPose : MonoBehaviour
{
    HardwareHand hardwareHand;
    public bool useDefaultOffsets = true;
    public Vector3 handPositionOffset = Vector3.zero;
    public Quaternion handRotationOffset = Quaternion.identity;
    private void Start()
    {
        XRHandSubsystem m_Subsystem =
            XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

        m_Subsystem.updatedHands += UpdatedHands;
    }

    private void OnDestroy()
    {
        XRHandSubsystem m_Subsystem =
            XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

        if (m_Subsystem != null) m_Subsystem.updatedHands -= UpdatedHands;
    }

    private void Awake()
    {
        hardwareHand = GetComponentInParent<HardwareHand>();
        if (hardwareHand == null)
            Debug.LogError("Missing hardware hand");

        // Apply default values
        if(useDefaultOffsets && hardwareHand)
        {
            if (hardwareHand.side == RigPart.LeftController)
            {
                handPositionOffset = new Vector3(0, 0.05f, 0.05f);
                handRotationOffset = Quaternion.Euler(0, 60f, 300f);
            }
            else if (hardwareHand.side == RigPart.RightController)
            {
                handPositionOffset = new Vector3(0, 0.05f, 0.05f);
                handRotationOffset = Quaternion.Euler(0, 300f, 60f);
            }
        }
    }

    private void UpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags succesFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (enabled == false || gameObject.activeSelf == false) return;
        switch (updateType)
        {
            case XRHandSubsystem.UpdateType.Dynamic:
                XRHandSubsystem.UpdateSuccessFlags pose;
                XRHand hand;
                if (hardwareHand.side == RigPart.LeftController)
                {
                    pose = XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;
                    hand = subsystem.leftHand;
                }
                else
                {
                    pose = XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;
                    hand = subsystem.rightHand;
                }
                var handavailable = (succesFlags & pose) != 0;
                if (handavailable)
                {
                    var wristJoint = hand.GetJoint(XRHandJointID.Wrist);

                    if (wristJoint.TryGetPose(out var wristPose))
                    {
                        hardwareHand.transform.localRotation = wristPose.rotation * handRotationOffset;
                        hardwareHand.transform.localPosition = wristPose.position + handPositionOffset;
                    }
                }
                break;
        }
    }
}

