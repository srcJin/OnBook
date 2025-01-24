using System.Collections.Generic;
using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using UnityEngine.InputSystem;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared;
using Fusion;
using Fusion.Addons.TextureDrawing;


/***
 * 
 *  StickyNoteColorSelection is in charged to sync the StickyNote color modification.
 *          
 ***/
public class StickyNoteColorSelection : GrabbableColorSelection
{
    TextureSurface texture;

    protected override void Awake()
    {
        base.Awake();
        texture = GetComponent<TextureSurface>();
        if (texture == null)
            Debug.LogError("TextureSurface not found");
    }

    protected override void ApplyColorChange(Color color)
    {
        texture.ChangeBackgroundColor(color);
    }
}
