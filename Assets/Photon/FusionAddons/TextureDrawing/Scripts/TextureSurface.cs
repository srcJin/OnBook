using Fusion.Addons.DataSyncHelpers;
using ProtoTurtle.BitmapDrawing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.TextureDrawing
{

    /***
     * 
     * `TextureSurface` references the `Renderer` component and contains the utility methods for textures editing : initialize the texture, change the texture color, draw a point, draw a line.
     * So, this class is not linked to the network part.
     * 
     * The addon provides two ways to edit a texture: 
     *    - either a texture is edited using the `ProtoTurtle.BitmapDrawing` third party solution which provides a bitmap drawing API, 
     *    - or it is edited using a specific shader which is used to create a material. Then this material will be used and merged with the render texture thanks to the `Graphics.Blit` method. This solution is more suitable for high resolution textures.
     * 
     * It implements the `IRenderTextureProvider` interface (from the `DataSyncHelpers` addon). The `onRedrawRequired` event is raised when the texture has been edited externally.
     *
     ***/

    public class TextureSurface : MonoBehaviour, IRenderTextureProvider
    {
        public Renderer drawRenderer;


        public Color startColor = Color.clear;

        public int maxRes = 1024;
        public bool squareResolution = true;

        public int minPressureRadius = 2;
        public int maxPressureRadius = 8;

        public int TextureWidth => useShaderDrawing ? renderTexture.width : texture.width; 
        public int TextureHeight => useShaderDrawing ? renderTexture.height : texture.height;

        [Header("Texture apply drawing")]
        public int refreshRateRatio = 1;
        int refreshesSinceLastApply = 0;
        Texture2D texture;
        bool dirtyTexture = false;

        [Header("Shader drawing")]
        [SerializeField] bool useShaderDrawing = true;
        [SerializeField] Shader drawingShader;
        [SerializeField] RenderTexture referenceRenderTexture;
        [SerializeField] RenderTexture renderTexture;
        [Tooltip("If no referenceRenderTexture is provided to copy, a render texture will be created based on scale and maxRes. If usePoserOf2Res is true, the resulting render texture will use power of 2 width and height")]
        [SerializeField] bool usePoserOf2Res = true;
        Material drawingMaterial;

        public UnityEvent onRedrawRequired;

        Color lastResetColor;

        #region IRenderTextureProvider
        RenderTexture IRenderTextureProvider.RenderTexture => renderTexture;

        public void OnRenderTextureEdit()
        {
            RedrawRequired();
        }

        public void RedrawRequired() { 
            // The texture has been edited externally: we warn component using this one directly that their drawing might have been affected
            if (onRedrawRequired != null) onRedrawRequired.Invoke();
        }
        #endregion

        private void Awake()
        {
            lastResetColor = startColor;
            if (drawRenderer == null) drawRenderer = GetComponentInChildren<Renderer>();
            var scale = drawRenderer.transform.lossyScale;
            Vector2 textureScale = Vector3.zero;
            if (squareResolution)
            {
                textureScale = new Vector2(maxRes, maxRes);
            }
            else if (scale.x < scale.y)
            {
                textureScale = new Vector2(maxRes * scale.x / scale.y, maxRes);
            }
            else
            {
                textureScale = new Vector2(maxRes, maxRes * scale.y / scale.x);
            }

            if (useShaderDrawing)
            {
                InitShaderDrawing(textureScale);
            }
            else
            {
                InitTextureApplyDrawing(textureScale);
            }
        }

        private void OnDestroy()
        {
            if (texture)
                Destroy(texture);
            if (renderTexture)
                Destroy(renderTexture);
            if (drawingMaterial)
                Destroy(drawingMaterial);
        }

        protected virtual void Update()
        {
            if (useShaderDrawing == false)
            {
                TextureApplyDirty();
            }
        }

        public void AddPoint(Vector2 point, byte pressure, Color color)
        {
            int radius = RadiusForPressure(pressure);
            if (useShaderDrawing)
            {
                ShaderDrawPoint(point, color, radius);
            }
            else
            {
                TextureDrawPoint(point, radius, color);
            }
        }

        int RadiusForPressure(byte pressure)
        {
            int radius = (int)(minPressureRadius + (((float)pressure) / 255f) * (maxPressureRadius - minPressureRadius));
            return radius;
        }

        public void AddLine(Vector2 fromPoint, byte fromPressure, Color fromColor, Vector2 toPoint, byte toPressure, Color toColor)
        {
            int fromRadius = RadiusForPressure(fromPressure);
            int toRadius = RadiusForPressure(toPressure);
            if (useShaderDrawing)
            {
                ShaderDrawLine(fromPoint, toPoint, toColor, toRadius);
            }
            else
            {
                TextureDrawLine(fromPoint, fromRadius, fromColor, toPoint, toRadius, toColor);
            }
        }

        public void ChangeBackgroundColor(Color color, bool redraw = true)
        {
            if (useShaderDrawing)
            {
                ShaderReset(color, redraw);
            }
            else
            {
                TextureApplyReset(color, redraw);
            }
        }

        [ContextMenu("ResetTexture")]
        public void ResetTexture()
        {
            if (useShaderDrawing)
            {
                ShaderReset(lastResetColor);
            }
            else 
            {
                TextureApplyReset(lastResetColor);
            }
        }

        #region Shader drawing
        void InitShaderDrawing(Vector2 textureScale)
        {
            if (drawingShader == null)
            {
                drawingShader = Shader.Find("Shader Graphs/LinePainter");
            }
            if (drawingShader == null)
            {
                Debug.LogError("Missing shader");
                return;
            }
            drawingMaterial = new Material(drawingShader);
            if (referenceRenderTexture)
            {
                renderTexture = new RenderTexture(referenceRenderTexture);
            }
            else
            {
                if (usePoserOf2Res)
                {
                    var x = Mathf.NextPowerOfTwo((int)textureScale.x);
                    if (x > maxRes) 
                        x = Mathf.ClosestPowerOfTwo((int)textureScale.x);
                    var y = Mathf.NextPowerOfTwo((int)textureScale.y);
                    if (y > maxRes) 
                        y = Mathf.ClosestPowerOfTwo((int)textureScale.y);
                    textureScale = new Vector2(x, y);
                }
                renderTexture = new RenderTexture((int)textureScale.x, (int)textureScale.y, 0);
                renderTexture.filterMode = FilterMode.Bilinear;
                renderTexture.wrapMode = TextureWrapMode.Clamp;
                renderTexture.useMipMap = true;

            }
#if POLYSPATIAL_SDK_AVAILABLE
            // Depth stencil format bliting is currently incompatible with Polyspatial
            renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#endif
            ShaderReset(startColor);
            drawRenderer.material.mainTexture = renderTexture;
        }

        void ShaderReset(Color color, bool redraw = true)
        {
            lastResetColor = color;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            Graphics.Blit(tex, renderTexture);
            Destroy(tex);
#if POLYSPATIAL_SDK_AVAILABLE
            TextureChanged();
#endif
            if (redraw) RedrawRequired();
        }

#if POLYSPATIAL_SDK_AVAILABLE
        async void TextureChanged() {
            Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(renderTexture);
            // Currently, MarkDirty sometimes fails if done during the same frame as the actual edit
            await System.Threading.Tasks.Task.Delay(30);
            Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(renderTexture);
        }
#else
        void TextureChanged() {}
#endif

        void ShaderDrawPoint(Vector2 coord, Color color, float widthPx)
        {
            ShaderDrawLine(coord, coord, color, widthPx);
        }

        RenderTexture temporaryRenderTexture;

        void ShaderDrawLine(Vector2 fromCoord, Vector2 toCoord, Color color, float widthPx)
        {
            if (renderTexture == null) return;

            var lineStart = new Vector2(fromCoord.x / renderTexture.width, 1f - fromCoord.y / renderTexture.height);
            var lineEnd = new Vector2(toCoord.x / renderTexture.width, 1f - toCoord.y / renderTexture.height);
            var width = widthPx / maxRes;
            if(temporaryRenderTexture == null)
            {
                temporaryRenderTexture = RenderTexture.GetTemporary(renderTexture.descriptor);
            }
            
            drawingMaterial.SetVector("_LineStart", lineStart);
            drawingMaterial.SetVector("_LineEnd", lineEnd);
            drawingMaterial.SetVector("_LineColor", color);
            drawingMaterial.SetFloat("_LineWidth", width);
            Graphics.Blit(renderTexture, temporaryRenderTexture);
            // https://forum.unity.com/threads/graphics-blit-with-a-material-is-just-giving-me-a-big-blue-box.1401028/#post-8953461
            Graphics.Blit(temporaryRenderTexture, renderTexture, drawingMaterial, 0);
#if POLYSPATIAL_SDK_AVAILABLE
            TextureChanged();
#endif
        }
        #endregion

        private void LateUpdate()
        {
            if ( temporaryRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(temporaryRenderTexture);
            }
        }

        #region Texture apply
        void TextureApplyReset(Color color, bool redraw = true)
        {
            lastResetColor = color;
            texture.DrawFilledRectangle(new Rect(0, 0, texture.width, texture.height), color);
            if(redraw) texture.Apply();
            RedrawRequired();
        }

        void InitTextureApplyDrawing(Vector2 textureScale)
        {
            texture = new Texture2D((int)textureScale.x, (int)textureScale.y);
            drawRenderer.material.mainTexture = texture;
            TextureApplyReset(startColor);
        }

        void TextureApplyDirty()
        {
            if (dirtyTexture)
            {
                refreshesSinceLastApply++;
                if (refreshesSinceLastApply >= refreshRateRatio)
                {
                    texture.Apply();
                    refreshesSinceLastApply = 0;
                    dirtyTexture = false;
                }
            }
        }
        public void TextureDrawPoint(Vector2 point, int radius, Color color)
        {
            texture.DrawFilledCircle((int)point.x, (int)point.y, radius, color);
            dirtyTexture = true;
        }

        void TextureDrawLine(Vector2 fromPoint, int fromRadius, Color fromColor, Vector2 toPoint, int toRadius, Color toColor)
        { 
            float distance = Vector3.Distance(fromPoint, toPoint);
            float drawingSize = Mathf.Min(toRadius, fromRadius);
            int steps = (int)(distance / drawingSize);
            float stepSize = drawingSize / distance;
            float progress = 0;
            for(int i = 0; i <= steps; i++)
            {
                progress = i * stepSize;
                var pos = fromPoint + progress * (toPoint - fromPoint);
                var radius = Mathf.Lerp(fromRadius, toRadius, progress);
                texture.DrawFilledCircle((int)pos.x, (int)pos.y, (int)radius, toColor);
            }
            texture.DrawFilledCircle((int)toPoint.x, (int)toPoint.y, toRadius, toColor);
            dirtyTexture = true;
        }
        #endregion
    }

}
