
using Fusion.XRShared.Demo;
using System.Collections;
using UnityEngine;


public class StickyNoteDispenser : GrabbablePrefabSpawner
{

    [Header("StickyNoteDispenser")]
    [SerializeField] Transform stickyNoteTargetTransform;
    float animationDuration = 1f;

    [Tooltip("After sticky notes spawning, the drawing will be prevented through TextureDrawing for this duration")]
    [SerializeField] float drawingDesactivationDelay = 0;

    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(MoveStickyNote(spawnerGrabbableReference.transform, animationDuration));
    }


    protected override void ResetReferencePose()
    {
        base.ResetReferencePose();
        StartCoroutine(MoveStickyNote(spawnerGrabbableReference.transform,  animationDuration));
    }

    IEnumerator MoveStickyNote(Transform objTransform, float duration = 1f)
    {
        float timeElapsed = 0;
        Vector3 startPosition = stickyNoteTargetTransform.InverseTransformPoint(objTransform.position);

        while (timeElapsed < duration)
        {
        
            var position = Vector3.Lerp(startPosition, Vector3.zero, timeElapsed / duration);
            objTransform.position = stickyNoteTargetTransform.TransformPoint(position);

            timeElapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.position = stickyNoteTargetTransform.position;
    }

    protected override GameObject Spawn()
    {
        var note = base.Spawn();
        HandleDrawingDelay(note);
        return note;
    }

    async void HandleDrawingDelay(GameObject note)
    {
        if (drawingDesactivationDelay == 0 || note == null) return;
        var drawing = note.GetComponentInChildren<Fusion.Addons.TextureDrawing.TextureDrawing>();
        if (drawing == null) return;
        drawing.enabled = false;
        await Fusion.XR.Shared.AsyncTask.Delay((int)(1000 * drawingDesactivationDelay));
        drawing.enabled = true;
    }
}
