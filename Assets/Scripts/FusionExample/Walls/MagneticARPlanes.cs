using UnityEngine;


/***
 * 
 * The MagneticARPlanes class controls the visibility and opacity of a magnetic plane mesh renderer. 
 * It provides properties for controlling the visibility, visibility level, and maximum mesh opacity. 
 * The Awake() method stores the initial state of the mesh renderer.
 * The LateUpdate() method updates the mesh renderer's enabled state and opacity based on the visibility and visibility level. 
 * 
 ***/

public class MagneticARPlanes : MonoBehaviour
{
    MeshRenderer meshRenderer;

    [SerializeField]
    public bool visible = true;
    public float visibilityLevel = 1;
    float lastVisibilityLevel = 1;
    bool meshRendererEnabled = false;
    public float maxMeshOpacity = 0.6f;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRendererEnabled = meshRenderer.enabled;
    }

    private void LateUpdate()
    {
        meshRenderer.enabled = visible && meshRendererEnabled;
        if (visible && lastVisibilityLevel != visibilityLevel)
        {
            var meshOpacity = visibilityLevel * maxMeshOpacity;
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, meshOpacity);
            lastVisibilityLevel = visibilityLevel;
        }
    }
}
