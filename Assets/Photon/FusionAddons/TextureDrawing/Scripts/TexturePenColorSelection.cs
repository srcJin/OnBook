using Fusion.XR.Shared.Grabbing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.TextureDrawing
{
    [Obsolete("Use GrabbableColorSelection directly", false)]
    public class TexturePenColorSelection : GrabbableColorSelection
    {
        TexturePen texturePen;
        [SerializeField] List<Renderer> penColoredRenderers = new List<Renderer>();

        protected override void FillColoredRenderers()
        {
            coloredRenderers = penColoredRenderers;
        }

        protected override void ApplyColorChange(Color color)
        {
            // Change coloredRenderers color
            base.ApplyColorChange(color);
            // Change pen used color
            texturePen.color = color;
        }

        protected override void Awake()
        {
            base.Awake();
            texturePen = GetComponent<TexturePen>();
            if (texturePen == null)
                Debug.LogError("TextureDetector not found");
        }
    }
}
