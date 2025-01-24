using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSG : MonoBehaviour
{
    [SerializeField] RenderTexture rt;
    [SerializeField] Shader drawingShader;
    Material drawingMaterial;
    [SerializeField] Renderer displayRenderer;

    private void Awake()
    {
        drawingMaterial = new Material(drawingShader);
        rt = new RenderTexture(rt);
#if POLYSPATIAL_SDK_AVAILABLE
        // Depth stencil format bliting is currently incompatible with Polyspatial
        rt.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#endif
        ResetRT(Color.yellow);
        displayRenderer.material.mainTexture = rt;

        DrawPoint(new Vector2(0.5f, 0.5f), Color.black);
    }

    private void OnDestroy()
    {
        Destroy(drawingMaterial);
        Destroy(rt);
    }

    Vector2 lastPos = Vector2.zero;
    private void Update()
    {
        var min = -0.01f;
        var max = 0.011f;
        var newPos = new Vector2(Mathf.Clamp01(lastPos.x + Random.Range(min, max)), Mathf.Clamp01(lastPos.y + Random.Range(min, max)));
        Draw(lastPos, newPos, Color.red, Random.Range(0.0001f, 0.01f));
        lastPos = newPos;
    }

    void ResetRT(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        Graphics.Blit(tex, rt);
#if POLYSPATIAL_SDK_AVAILABLE
        Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(rt);
#endif
        Destroy(tex);
    }

    void DrawPoint(Vector2 coord, Color color, float width = 0.004f)
    {
        Draw(coord, coord, color, width);
    }

    void Draw(Vector2 fromCoord, Vector2 toCoord, Color color, float width = 0.004f) { 
        var temporaryRenderTexture = RenderTexture.GetTemporary(rt.descriptor);
        drawingMaterial.SetVector("_LineStart", fromCoord);
        drawingMaterial.SetVector("_LineEnd", toCoord);
        drawingMaterial.SetVector("_LineColor", color);
        drawingMaterial.SetFloat("_LineWidth", width);
        Graphics.Blit(rt, temporaryRenderTexture);
        // https://forum.unity.com/threads/graphics-blit-with-a-material-is-just-giving-me-a-big-blue-box.1401028/#post-8953461
        Graphics.Blit(temporaryRenderTexture, rt, drawingMaterial, 0);
        RenderTexture.ReleaseTemporary(temporaryRenderTexture);
#if POLYSPATIAL_SDK_AVAILABLE
        Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(rt);
#endif
    }
}
