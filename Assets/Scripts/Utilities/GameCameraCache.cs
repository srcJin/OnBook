using UnityEngine;

/// <summary>
/// The purpose of this class is to provide a cached reference to the main camera. Calling Camera.main
/// executes a FindByTag on the scene, which will get worse and worse with more tagged objects.
/// </summary>
public static class GameCameraCache
{
    private static GameObject cachedCameraGameObject;

    /// <summary>
    /// Any class can call: CameraCache.MainGameObject to obtain the game object that has the MainCamera tag.
    /// </summary>
    public static GameObject MainGameObject
    {
        get
        {
            if (cachedCameraGameObject == null)
            {
                GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");

                for (int i = 0; i < mainCameras.Length; i++)
                {
                    if (mainCameras[i].activeSelf)
                    {
                        cachedCameraGameObject = mainCameras[i];
                        break;
                    }
                }

                return cachedCameraGameObject;
            }
            return cachedCameraGameObject;
        }
    }
}