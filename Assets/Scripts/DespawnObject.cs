using Fusion;
using Fusion.XR.Shared;
using System.Collections;
using UnityEngine;

/***
 * 
 * The DespawnObject class is responsible for managing the removal of networked objects. 
 * It provides functionality to request state authority over an object before removing it, ensuring that the correct player has control. 
 * The class also handles audio feedback and includes a customizable delay before despawning the object. 
 * 
 ***/
public class DespawnObject : NetworkBehaviour
{
    [Header("Feedback")]
    [SerializeField] IFeedbackHandler feedback;
    [SerializeField] string audioType;
    [SerializeField] float delayBeforeDespawn=0.6f;

    private void Awake()
    {
        if (feedback == null)
            feedback = GetComponent<IFeedbackHandler>();
    }

    public async void DeleteObject()
    {
        await Object.WaitForStateAuthority();
        if (Object.HasStateAuthority)
        {
            StartCoroutine(DeleteObjectAfterDelay(delayBeforeDespawn));
        }
    }

    IEnumerator DeleteObjectAfterDelay(float delay)
    {
        if (feedback != null)
        {
            feedback.PlayAudioFeeback(audioType);
        }
        
        yield return new WaitForSeconds(delay);
        Object.Runner.Despawn(this.Object);
    }
}
