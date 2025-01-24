using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotoCamera : PhotoRecorder
{
    public InputActionProperty leftUseAction;
    public InputActionProperty rightUseAction;
    public float minAction = 0.05f;
    public float delayBetweenShot = 1.5f;
    protected NetworkGrabbable grabbable;
    InputActionProperty UseAction => IsGrabbed && grabbable.CurrentGrabber.hand && grabbable.CurrentGrabber.hand.side == RigPart.LeftController ? leftUseAction : rightUseAction;
    public virtual bool IsGrabbed => grabbable && grabbable.IsGrabbed;
    public virtual bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable && grabbable.CurrentGrabber && grabbable.CurrentGrabber.Object.StateAuthority == Runner.LocalPlayer;
    public virtual bool IsUsed => UseAction.action.ReadValue<float>() > minAction;

    float lastShot = 0;

    [SerializeField] Renderer cameraOffScreen;

    protected override void Awake()
    {
        base.Awake();
        grabbable = GetComponent<NetworkGrabbable>();

        leftUseAction.EnableWithDefaultXRBindings(new List<string> { "<XRController>{LeftHand}/trigger", "<Keyboard>/space" });
        rightUseAction.EnableWithDefaultXRBindings(new List<string> { "<XRController>{RightHand}/trigger", "<Keyboard>/space" });
        ShutDownScreen();

        grabbable.onDidGrab.AddListener(EnableScreen);
        grabbable.onDidUngrab.AddListener(ShutDownScreen);
    }

    private void EnableScreen(NetworkGrabber g)
    {
        cameraOffScreen.enabled = false;
        foreach (var mirrorRenderer in mirrorRenderers)
        {
            mirrorRenderer.enabled = true;
        }
        captureCamera.enabled = true;
    }
    private void ShutDownScreen()
    {
        cameraOffScreen.enabled = true;
        foreach (var mirrorRenderer in mirrorRenderers)
        {
            mirrorRenderer.enabled = false;
        }
        captureCamera.enabled = false;
    }

    private void Update()
    {
        if (IsUsed && IsGrabbedByLocalPLayer && (Time.time - lastShot) > delayBetweenShot)
        {
            lastShot = Time.time;
            var picture = CreatePicture();
        }
    }
}
