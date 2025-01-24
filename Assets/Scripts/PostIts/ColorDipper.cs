using UnityEngine;


/***
 * 
 *  The ColorDipper class is responsible for retrieving and storing the color of a button's material. 
 *  
 ***/

public class ColorDipper : MonoBehaviour
{

    MeshRenderer meshRenderer;
    public Color color;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if(meshRenderer != null && meshRenderer.material != null)
        {
            color = meshRenderer.material.color;
        }
    }
}
