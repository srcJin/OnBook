using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10_000)]
public class HideRenderers : MonoBehaviour
{
    List<Renderer> renderers = new List<Renderer>();

    void Awake()
    {
        renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
    }

    void LateUpdate()
    {
        foreach (var r in renderers)
        {
            if(r) r.enabled = false;
        }
    }
}
