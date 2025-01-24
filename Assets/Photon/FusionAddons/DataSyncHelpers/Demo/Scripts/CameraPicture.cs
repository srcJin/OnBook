using Fusion;
using TMPro;
using UnityEngine;
using Fusion.Addons.DataSyncHelpers;
using System;

public class CameraPicture : StreamSynchedBehaviour
{
    Texture2D texture;
    [Tooltip("Renderer to which the captured texture will be applie. Not used if a TargetRenderTexture is found")]
    [SerializeField] MeshRenderer pictureRenderer;
    [SerializeField] Material actualPictureMaterial;
    [SerializeField] Vector2 renderTextureCopyScale = new Vector2(1, 1);
    [SerializeField] Vector2 renderTextureCopyOffset = new Vector2(0, 0);
    [SerializeField] TextMeshPro progressText;
    IRenderTextureProvider renderTextureProvider;

    private void Awake()
    {
        if (pictureRenderer == null) pictureRenderer = GetComponentInChildren<MeshRenderer>();
        if (pictureRenderer == null) Debug.LogError("pictureRenderer not set!");
        if (progressText != null) progressText.gameObject.SetActive(false);
        renderTextureProvider = GetComponentInChildren<IRenderTextureProvider>();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        ResetTexture();
    }

    public void SetPictureTexture(RenderTexture renderTexture)
    {
        if (Object.HasStateAuthority && texture == null)
        {
            SaveRenderTexturePixels(renderTexture);

            if (TargetRenderTexture == null)
            {
                if (actualPictureMaterial != null)
                {
                    pictureRenderer.material = actualPictureMaterial;
                }
                pictureRenderer.material.mainTexture = texture;
            }                

            // Send the texture to all users
            Send(ByteArrayTools.TextureData(renderTexture));
        }
    }

    public RenderTexture TargetRenderTexture => renderTextureProvider != null ? renderTextureProvider.RenderTexture : null;

    protected override void OnDataProgress(float progress)
    {
        base.OnDataProgress(progress);
        if (progressText)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"{(int)(100 * progress)}%";
        }
    }

    public override void OnDataChunkReceived(byte[] data, PlayerRef source, float time)
    {
        Debug.Log($"[{Object.Id}] Image received "+data.Length);
        if (progressText)
            progressText.gameObject.SetActive(false);
        base.OnDataChunkReceived(data, source, time);
        if(TargetRenderTexture != null)
        {
            Texture2D imageTexture = null;
            ByteArrayTools.FillTexture(ref imageTexture, data);
            Graphics.Blit(imageTexture, TargetRenderTexture);
            Destroy(imageTexture);
            if (renderTextureProvider != null) renderTextureProvider.OnRenderTextureEdit();
        }
        else
        {
            ByteArrayTools.FillTexture(ref texture, data);
            if (actualPictureMaterial != null)
            {
                pictureRenderer.material = actualPictureMaterial;
            }
            pictureRenderer.material.mainTexture = texture;
        }
    }

    #region Texture handling
    void SaveRenderTexturePixels(RenderTexture renderTexture)
    {
        // Ensure that any pre-existing allocated texture is freed
        ResetTexture();

        var temporaryRenderTexture = RenderTexture.GetTemporary(renderTexture.descriptor);
        Graphics.Blit(renderTexture, temporaryRenderTexture, renderTextureCopyScale, renderTextureCopyOffset);
        Graphics.Blit(temporaryRenderTexture, renderTexture);
        RenderTexture.ReleaseTemporary(temporaryRenderTexture);

        if (TargetRenderTexture != null)
        {
            Graphics.Blit(renderTexture, TargetRenderTexture);
        }
        else
        {
            texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, true);
            var currentRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = currentRenderTexture;
        }
    }

    public void ResetTexture()
    {
        if (texture)
        {
            Destroy(texture);
        }
        texture = null;
    }
    #endregion

}

