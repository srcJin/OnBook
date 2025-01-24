using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PhotoRecorder : NetworkBehaviour
{
    [SerializeField] NetworkObject picturePrefab;
    [SerializeField] protected Camera captureCamera;
    [SerializeField] protected List<Renderer> mirrorRenderers = new List<Renderer>();

    protected virtual void Awake()
    {
        if (captureCamera == null) captureCamera = GetComponentInChildren<Camera>();
        if (captureCamera == null || captureCamera.targetTexture == null)
        {
            Debug.LogError("A camera with a render texture target should be provided");
        }
        else
        {
            captureCamera.targetTexture = new RenderTexture(captureCamera.targetTexture);
            foreach(var mirrorRenderer in mirrorRenderers)
            {
                mirrorRenderer.material.mainTexture = captureCamera.targetTexture;
            }
        }
    }

    private void OnDestroy()
    {
        if(captureCamera && captureCamera.targetTexture)
        {
            Destroy(captureCamera.targetTexture);
        }
    }

    [EditorButton("Shoot picture")]
    public void ShootPicture()
    {
        Debug.Log("OnCameraShoot");
        CreatePicture();
    }

    public virtual CameraPicture CreatePicture()
    {
        var pictureGO = Runner.Spawn(picturePrefab, transform.position, transform.rotation, Runner.LocalPlayer);
        var picture = pictureGO.GetComponent<CameraPicture>();
        captureCamera.Render();
        picture.SetPictureTexture(captureCamera.targetTexture);
        return picture;
    }
}
