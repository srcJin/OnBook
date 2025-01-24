using Fusion.Addons.BlockingContact;
using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.TextureDrawing
{
    /***
     * 
     * The `TexturePen` is located on the pen (with a `BlockableTip` component) and try to detect a contact with a `TextureDrawing` (with a `BlockingSurface` component).
     * When a contact is detected, the local list of points to draw is updated. Then for each point, the method `AddDrawingPoint()` of `TextureDrawer` is called during the `FixedUpdateNetwork()`.
     * 
     ***/

    public class TexturePen : NetworkBehaviour
    {
        BlockableTip blockableTip;
        TextureDrawer textureDrawer;

        BlockingSurface lastBlockingsurface;
        TextureDrawing lastTextureDrawing;
        bool isDrawing = false;

        public Color color = Color.black;

        IColorProvider colorProvider;
        IFeedbackHandler feedback;

        [Header("Feedback")]
        [SerializeField] string audioType;
        [SerializeField] float hapticAmplitudeFactor = 0.1f;
        [SerializeField] FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic;

        public struct PendingDrawingPoint 
        {
            public Vector2 position;
            public Color color;
            public byte pressureByte;
            public TextureDrawing drawing;
            public bool alreadyDrawn;
        }

        private void Awake()
        {
            feedback = GetComponent<IFeedbackHandler>();
            blockableTip = GetComponent<BlockableTip>();
            textureDrawer = GetComponent<TextureDrawer>();
            colorProvider = GetComponent<IColorProvider>();
        }

        // Update is called once per frame
        void Update()
        {
            if(Object && colorProvider != null)
                color = colorProvider.CurrentColor;

            if (Object == null ||  Object.HasStateAuthority == false) return;

            TextureDrawing previousTextureDrawing = lastTextureDrawing;
            bool wasDrawing = isDrawing;

            lastTextureDrawing = null;
            isDrawing = false;

            if (blockableTip.IsContactAllowed && blockableTip.IsInContact && blockableTip.lastSurfaceInContact != null)
            {
                TextureDrawing currentDrawing = null;

                if (blockableTip.lastSurfaceInContact != lastBlockingsurface || previousTextureDrawing == null)
                {
                    lastBlockingsurface = blockableTip.lastSurfaceInContact;
                    currentDrawing = blockableTip.lastSurfaceInContact.GetComponentInParent<TextureDrawing>();
                }
                else
                {
                    currentDrawing = previousTextureDrawing;
                }
                    

                if (currentDrawing)
                {
                    lastTextureDrawing = currentDrawing;
                    isDrawing = true;

                    float blockableTipPressure = 0;
                    if (blockableTip.lastSurfaceInContact.maxDepth == 0)
                    {
                        blockableTipPressure = 1;
                    }
                    else
                    {
                        var depth = blockableTip.lastSurfaceInContact.referential.InverseTransformPoint(blockableTip.tip.position).z;
                        blockableTipPressure = Mathf.Clamp01(1f - ((blockableTip.lastSurfaceInContact.maxDepth - depth) / blockableTip.lastSurfaceInContact.maxDepth));
                    }

                    byte pressure = (byte)(1 + (byte)(254 * blockableTipPressure));
                    var coordinate = blockableTip.SurfaceContactCoordinates;
                    var surface = lastTextureDrawing.textureSurface;
                    Vector2 textureCoord = new Vector2(surface.TextureWidth * (coordinate.x + 0.5f), surface.TextureHeight * (0.5f - coordinate.y));

                    textureDrawer.AddPointWithThrottle(textureCoord, pressure, color, lastTextureDrawing);

                    if (feedback != null )
                    {
                        feedback.PlayAudioAndHapticFeeback(audioType: audioType, audioOverwrite: false, hapticAmplitude: Mathf.Clamp01(hapticAmplitudeFactor * blockableTipPressure), feedbackMode: feedbackMode);
                    }
                }
            } 

            if (wasDrawing && previousTextureDrawing != null && lastTextureDrawing != previousTextureDrawing)
            {
                // Add stop point
                textureDrawer.AddStopDrawingPointWithThrottle(previousTextureDrawing);
            }

            if(wasDrawing && isDrawing == false && feedback != null)
            {
                feedback.StopAudioFeeback();
            }
        }
    }

}

