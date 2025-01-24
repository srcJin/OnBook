using Fusion.Addons.VisionOsHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/***
 * 
 * The WallVisibilityHandler class is used to handle the visibility and opacity of walls in a scene. 
 * The selection button located in the center of wall is resized in the Start() to counteract the parent's scale modification when wall prefab is resized.
 * Walls can have 3 status : PendingMainWallSelection, MainWall, BaseWall.
 * All walls are stored in a static dictionnary SceneWalls.
 * If a wall is selected by the user, the ElectAsMainWall() method is called and all wall status is updated. Then, only the main wall is visible.
 * The class also implements the IProximityTarget interface to handles proximity to a ProximityHighlighter (MagnetPoint).
 * It provides two kind of effects to control the visibility level of the wall (increasing the alpha or colors' intensity), and applies these effects based on distance with the ProximityHighlighter. 
 * 
 ***/
public class WallVisibilityHandler : MonoBehaviour, IProximityTarget
{
    static public List<WallVisibilityHandler> SceneWalls = new List<WallVisibilityHandler>();
    MeshRenderer meshRenderer;
    [SerializeField] SpatialButton selectionButton;
    [SerializeField] bool hideButtonAfterMainWallSelection = true;
    [SerializeField] SpaceLocalizationHandler spaceLocalizationHandler;

    private Vector3 selectionButtonOriginalScale;
    private RectTransform selectionButtonRectTransform;

    [SerializeField] private Color initialColor;

    public enum EffectKind { 
        IncreaseAlpha,
        IncreaseVisibleColoursIntensity
    }

    [Header("Visibility level")]
    [SerializeField] EffectKind effectKind = EffectKind.IncreaseVisibleColoursIntensity;
    [SerializeField] float maxEffectValue = 0.6f;
    [SerializeField] float minVisibilityLevelDuringProximity = 0.1f;

    [SerializeField] bool useBaseEffectDuringPendingSelection = true;
    [SerializeField] bool useBaseEffectDuringMainWall = true;
    [SerializeField] bool useBaseEffectDuringBaseWall = false;

    private float visibilityLevel = 0;
    private float lastVisibilityLevel = -1;


    public enum Status
    {
        PendingMainWallSelection,
        MainWall,
        BaseWall,
    }
    public Status status = Status.PendingMainWallSelection;

    void Awake()
    {
        SceneWalls.Add(this);
        meshRenderer = GetComponent<MeshRenderer>();

        // Find button, in child or in parent childs
        if (selectionButton == null)
        {
            selectionButton = GetComponentInChildren<SpatialButton>();
            if (selectionButton == null && transform.parent)
            {
                selectionButton = transform.parent.GetComponentInChildren<SpatialButton>();
            }
        }


        if (selectionButton != null)
        {
            selectionButtonRectTransform = selectionButton.GetComponent<RectTransform>();
            selectionButtonOriginalScale = selectionButtonRectTransform.localScale;
        }

        initialColor = meshRenderer.material.color;

        spaceLocalizationHandler = GetComponentInChildren<SpaceLocalizationHandler>();

        CheckMainWall();
    }

    private void Start()
    {
        if (selectionButton != null)
        {
            Vector3 parentScale = transform.parent.localScale;

            var buttonAngle = Vector3.Angle(Vector3.up, selectionButton.transform.up);
            if (buttonAngle > 20f)
            {
                selectionButtonRectTransform.localRotation = Quaternion.Euler(0, 0, buttonAngle);
                // Adjust the button's scale to counteract the parent's scale
                selectionButtonRectTransform.localScale = new Vector3(
                    selectionButtonOriginalScale.x / parentScale.y,
                    selectionButtonOriginalScale.y / parentScale.x,
                    selectionButtonOriginalScale.z / parentScale.z
                );
            }
            else
            {
                selectionButtonRectTransform.localScale = new Vector3(
                    selectionButtonOriginalScale.x / parentScale.x,
                    selectionButtonOriginalScale.y / parentScale.y,
                    selectionButtonOriginalScale.z / parentScale.z
                );
            }
        }
        ApplyVisibilityLevel();
    }

    private void LateUpdate()
    {
        // Need to be done everyframe, as ARPlaneMeshVisualizer might reenable a mesh that should be hidden
        UpdateVisibilityWithStatus();
    }

    private void OnDestroy()
    {
        SceneWalls.Remove(this);
    }

    #region Main wall detection

    public static WallVisibilityHandler FindMainWall() {
        var referenceWall = FindObjectOfType<ReferenceWall>();

        if (referenceWall == null) return null;

        float minDistance = float.PositiveInfinity;
        WallVisibilityHandler mainWall = null;

        foreach (var otherWall in SceneWalls)
        {
            var distance = Vector3.Distance(otherWall.transform.position, referenceWall.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                mainWall = otherWall;
            }
        }
        return mainWall;
    }

    public static void TriggerGlobalOnMainWallSelected()
    {
        var mainWall = FindMainWall();

        if (mainWall)
        {
            foreach (var otherWall in SceneWalls)
            {
                otherWall.OnMainWallSelected(mainWall);
            }
        }
    }

    public void CheckMainWall() {
        var mainWall = FindMainWall();
        if (mainWall)
        {
            OnMainWallSelected(mainWall);
        }
    }

    public void OnMainWallSelected(WallVisibilityHandler mainWall)
    {
        if (this == mainWall)
        {
            ChangeStatus(Status.MainWall);
        }
        else
        {
            ChangeStatus(Status.BaseWall);
        }
    }

    [ContextMenu("ElectAsMainWall")]
    public void ElectAsMainWall()
    {
        ChangeStatus(Status.MainWall);
        foreach (var otherWall in SceneWalls)
        {
            if (otherWall == this) continue;
            otherWall.ChangeStatus(Status.BaseWall);
        }

        if (spaceLocalizationHandler != null)
            spaceLocalizationHandler.ReferenceWallSelection();
    }

    public void ChangeStatus(Status s)
    {
        status = s;
        UpdateVisibilityWithStatus();
    }
    #endregion

    float EffectIntensity(float level, float initialValue, bool shouldAddInitialValue, bool keepZeroInitialValue = true)
    {
        float value;
        if (keepZeroInitialValue && initialValue == 0)
        {
            value = 0;
        }
        else if (shouldAddInitialValue)
        {
            value = Mathf.Clamp(initialValue + (maxEffectValue - initialValue) * level, 0, maxEffectValue);
        } 
        else
        {
            value = Mathf.Clamp(level * maxEffectValue, 0, maxEffectValue);
        }
        return value;
    }

    void ApplyVisibilityLevel()
    {
        if (lastVisibilityLevel != visibilityLevel)
        {
            var addInitialColor = ShouldBaseEffectBeAdded();
            if (effectKind == EffectKind.IncreaseAlpha)
            {
                meshRenderer.material.color = new Color(
                    meshRenderer.material.color.r, 
                    meshRenderer.material.color.g, 
                    meshRenderer.material.color.b, 
                    EffectIntensity(visibilityLevel, initialColor.a, addInitialColor, keepZeroInitialValue:false)
                );
            }
            else
            {
                meshRenderer.material.color = new Color(
                        EffectIntensity(visibilityLevel, initialColor.r, addInitialColor),
                        EffectIntensity(visibilityLevel, initialColor.g, addInitialColor),
                        EffectIntensity(visibilityLevel, initialColor.b, addInitialColor),
                        meshRenderer.material.color.a
                );
            }

            lastVisibilityLevel = visibilityLevel;
        }
    }

    bool ShouldBaseEffectBeAdded()
    {
        switch (status)
        {
            case Status.PendingMainWallSelection: return useBaseEffectDuringPendingSelection;
            case Status.MainWall: return useBaseEffectDuringMainWall;
            case Status.BaseWall: return useBaseEffectDuringBaseWall;
            default:return false;
        }
    }

    void UpdateVisibilityWithStatus()
    {
        var shouldDisplay = ShouldBaseEffectBeAdded() || visibilityLevel != 0;
        if (shouldDisplay != meshRenderer.enabled) {
            meshRenderer.enabled = shouldDisplay;
        }

        if (selectionButton)
        {
            selectionButton.gameObject.SetActive(status == Status.PendingMainWallSelection || !hideButtonAfterMainWallSelection);
        }
        ApplyVisibilityLevel();
    }

    #region IProximityTarget
    public void ProximityStart(ProximityHighlighter proximityHighlighter, float startDistance)
    {
        visibilityLevel = minVisibilityLevelDuringProximity;
        meshRenderer.enabled = true;
        ApplyVisibilityLevel();
    }

    public void ProximityChange(ProximityHighlighter proximityHighlighter, float startDistance, float distance)
    {
        var level = Mathf.Clamp((startDistance - distance) / startDistance, minVisibilityLevelDuringProximity, 1);
        visibilityLevel = level;
        meshRenderer.enabled = true;
        ApplyVisibilityLevel();
    }

    public void ProximityEnd(ProximityHighlighter proximityHighlighter)
    {
        visibilityLevel = 0;
        UpdateVisibilityWithStatus();
    }  
    #endregion
}