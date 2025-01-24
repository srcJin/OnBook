using System.Collections.Generic;
using Fusion.Addons.TextureDrawing;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared.Touch;
using UnityEngine;


/***
 * 
 * The TouchDrawing class implements the ITouchable interface to handle touch input in order to draw on a texture surface (TextureDrawing). 
 * It implements a detection system to know whether the user wishes to draw or grab the object.
 * It also includes functionality for smoothing toucher positions, and providing audio feedback when the user is drawing. 
 * The class also handles finding the appropriate TextureDrawer for drawing and includes a dictionary to cache toucher positions and drawers for each toucher. 
 * 
 ***/
public class TouchDrawing : MonoBehaviour, ITouchable
{
    TextureDrawing drawing;
    float maxDepth = 0.1f;
    public int numberOfToucherPositionsToSmoothOn = 5;
    Dictionary<Toucher, List<Vector3>> toucherPositionsByToucher = new Dictionary<Toucher, List<Vector3>>();
    Dictionary<Toucher, TextureDrawer> drawerByToucher = new Dictionary<Toucher, TextureDrawer>();
    Dictionary<Toucher, Grabber> grabberByToucher = new Dictionary<Toucher, Grabber>();
    Grabbable grabbable;
    bool isDrawing = false;

    int pointsToSkipToDetectGrabbing = 3;
    int skippedPoints = 0;

    public Color drawingColor = Color.black;

    [Header("Feedback")]
    [SerializeField] IFeedbackHandler feedback;
    [SerializeField] string audioType;
    [SerializeField] float hapticAmplitudeFactor = 0.001f;
    [SerializeField] FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic;


    public enum FingerStatus
    {
        Drawing,
        Grabbing,
        None
    }

    protected FingerStatus currentFingerStatus = FingerStatus.None;
    protected List<TextureDrawer> activeDrawers = new List<TextureDrawer>();

    [Header("Grabbing / Drawing detection")]
    [SerializeField] Vector3 frontDirection = Vector3.forward;
    [SerializeField] float forwardDotProduct;
    [SerializeField] float directionThreshold = 0.6f; // Threshold for determining if the directions are considered "the same"

    protected virtual bool IsIndexToucherDrawingModeEnabled => true;

    private void Awake()
    {
        grabbable = GetComponentInChildren<Grabbable>();

        if (feedback == null)
            feedback = GetComponent<IFeedbackHandler>();
    }

    #region ITouchable
    void RemoveActiveDrawer(TextureDrawer drawer)
    {
        activeDrawers.Remove(drawer);
        if(activeDrawers.Count == 0)
        {
            currentFingerStatus = FingerStatus.None;
        }
    }

    void AddActiveDrawer(TextureDrawer drawer)
    {
        currentFingerStatus = FingerStatus.Drawing;
        if (activeDrawers.Contains(drawer)) return;
        activeDrawers.Add(drawer);
    }

    float maxContactDepthForDrawingStart = 0.01f;

    // Return true if triggers a drawing
    protected bool DetectGrabbingOrDrawingStart(Vector3 fingerTipPosition, Vector3 fingerForward)
    { 
        var fingerBackPosition = fingerTipPosition - fingerForward;
        // compute if the finger is touching at the front of the box collider
        Vector3 contactVector = fingerBackPosition - transform.position;
        float dotProduct = Vector3.Dot(contactVector, transform.TransformDirection(frontDirection));

        // to compare the finger and the sticky note directions 
        forwardDotProduct = Vector3.Dot(fingerForward, transform.forward);

        var positPlane = new Plane(transform.forward, transform.position);
        var contactPoint = positPlane.ClosestPointOnPlane(fingerTipPosition);
        var distance = Vector3.Distance(fingerTipPosition, contactPoint);


        if (dotProduct > 0)
        {
            if (currentFingerStatus == FingerStatus.None)
            {
                currentFingerStatus = FingerStatus.Grabbing;
            }
        }
        else if (dotProduct < 0 && forwardDotProduct > directionThreshold)
        {
            if (distance < maxContactDepthForDrawingStart && currentFingerStatus != FingerStatus.Grabbing)
            {
                currentFingerStatus = FingerStatus.Drawing;
                return true;
            }
        }
        return false;
    }

    public void OnToucherContactStart(Toucher toucher)
    {
        if (IsIndexToucherDrawingModeEnabled == false) return;

        var drawer = FindDrawer(toucher);
        if (drawer == null)
        {
            // Not a hand
            return;
        }
        var fingerTipPosition = toucher.transform.position;
        var fingerForward = toucher.transform.forward;
        if (DetectGrabbingOrDrawingStart(fingerTipPosition, fingerForward))
        {
            AddActiveDrawer(drawer);
        }

        if (currentFingerStatus == FingerStatus.Drawing && activeDrawers.Contains(drawer))
        {
            Draw(toucher.transform.position, toucher);
            AudioFeedback(toucher);
        }
    }

    public void OnToucherStay(Toucher toucher)
    {
        if (IsIndexToucherDrawingModeEnabled == false) return;

        var drawer = FindDrawer(toucher);
        if (drawer == null)
        {
            // Not a hand
            return;
        }

        if (currentFingerStatus == FingerStatus.Drawing && activeDrawers.Contains(drawer))
        {
            Draw(toucher.transform.position, toucher);
            AudioFeedback(toucher);
        }
    }
    
    public void OnToucherContactEnd(Toucher toucher)
    {
        var drawer = FindDrawer(toucher);
        if (drawer == null)
        {
            // Not a hand
            return;
        }

        if (IsIndexToucherDrawingModeEnabled == false) return;
        if (toucherPositionsByToucher.ContainsKey(toucher))
        {
            toucherPositionsByToucher[toucher].Clear();
        }

        EndLine(toucher);
        AudioFeedback(toucher);

        RemoveActiveDrawer(drawer);
    }
    #endregion

    protected virtual List<Vector3> PositionCache(Toucher toucher = null)
    {
        if (toucher == null) throw new System.Exception("Missing toucher");
        if (toucherPositionsByToucher.ContainsKey(toucher) == false)
            toucherPositionsByToucher[toucher] = new List<Vector3>();
        return toucherPositionsByToucher[toucher];
    }

    Vector3 SmoothPosition(Vector3 position, Toucher toucher) {
        List<Vector3> positionCache = PositionCache(toucher);
        if (numberOfToucherPositionsToSmoothOn > 1)
        {
            positionCache.Add(position);
            if (positionCache.Count > numberOfToucherPositionsToSmoothOn) positionCache.RemoveAt(0);
            var toucherAveragePosition = Vector3.zero;
            foreach (var toucherPos in positionCache) toucherAveragePosition += toucherPos;
            toucherAveragePosition = toucherAveragePosition / positionCache.Count;
            position = toucherAveragePosition;
        }
        return position;
    }

    byte pressure = 0;
    protected void Draw(Vector3 position, Toucher toucher = null)
    {
        position = SmoothPosition(position, toucher);

        if (TryLookForDrawingComponents(out var drawer, toucher) == false)
        {
            Debug.LogError($"Unable to draw due to missing component");
            return;
        }

        if (CanDraw(toucher) == false)
        {
            return;
        }
        if (skippedPoints < pointsToSkipToDetectGrabbing)
        {
            skippedPoints++;

            return;
        }
        isDrawing = true;

        var coordinate = drawing.textureSurface.transform.InverseTransformPoint(position);

        Vector2 textureCoord = new Vector2(drawing.textureSurface.TextureWidth * (coordinate.x + 0.5f), drawing.textureSurface.TextureHeight * (0.5f - coordinate.y));

        var depth = Mathf.Clamp01(coordinate.z / maxDepth);
        pressure = (byte)(1f + 254f * depth);
        drawer.AddDrawingPoint(textureCoord, pressure, drawingColor, drawing);
    }


    protected void EndLine(Toucher toucher = null)
    {
        if (isDrawing == false) return;
        if (TryLookForDrawingComponents(out var drawer, toucher) == false)
        {
            Debug.LogError($"Unable to draw due to missing component");
            return;
        }
        drawer.AddDrawingPoint(Vector3.zero, DrawingPoint.END_DRAW_PRESSURE, drawingColor, drawing);
        isDrawing = false;
        skippedPoints = 0;
    }

    protected virtual TextureDrawer FindDrawer(Toucher toucher)
    {
        if (toucher == null) return null;
        // Look for cached version
        if (drawerByToucher.ContainsKey(toucher))
        {
            return drawerByToucher[toucher];
        }
        // Find drawer in network rig, for the relevant hardware hand
        HardwareHand hand = toucher.GetComponentInParent<HardwareHand>();
        if (hand == null) return null;
        TextureDrawer drawer = null;
        foreach (var d in FindObjectsOfType<TextureDrawer>())
        {
            if (d.Object.HasStateAuthority)
            {
                NetworkHand networkHand = d.GetComponentInParent<NetworkHand>();
                if (hand != null && networkHand != null && hand.side == networkHand.side)
                {
                    drawer = d;
                    break;
                }
            }
        }
        if (drawer != null) {

            drawerByToucher[toucher] = drawer;
            var grabber = hand.GetComponentInChildren<Grabber>();
            if (grabber) {
                grabberByToucher[toucher] = grabber;
            }
        }
        return drawer;
    }

    bool TryLookForDrawingComponents(out TextureDrawer drawer, Toucher toucher = null)
    {
        if (drawing == null) drawing = GetComponent<TextureDrawing>();
        drawer = FindDrawer(toucher);
        if (drawer == null || drawing == null)
        {
            return false;
        }
        return true;
    }

    bool CanDraw(Toucher toucher)
    {
        if (grabbable && toucher != null && grabberByToucher.ContainsKey(toucher) && grabbable.currentGrabber == grabberByToucher[toucher])
        {
            // grabbed by the touching hand, we prevent drawing
            return false;
        }
        return true;
    }

    Toucher previousToucher = null;
    HardwareHand hand = null;
    protected void AudioFeedback(Toucher toucher = null)
    {
        if (feedback == null) return;

        if (toucher && (toucher != previousToucher || previousToucher == null))
        {
            hand = toucher.GetComponentInParent<HardwareHand>();
            previousToucher = toucher;
        }

        if (isDrawing)
        {
            var amplitude = (hand != null) ? Mathf.Clamp01(pressure * hapticAmplitudeFactor) : IHapticFeedbackHandler.USE_DEFAULT_VALUES;
            feedback.PlayAudioAndHapticFeeback(audioType: audioType, audioOverwrite: false, hapticAmplitude: amplitude, hardwareHand: hand, feedbackMode: feedbackMode);
        }
        else
        {
            feedback.PauseAudioFeeback();
        }
    }
}
