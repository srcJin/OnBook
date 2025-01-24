using Fusion;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using System.Collections.Generic;
using UnityEngine;


/***
 * 
 * The StickyNoteColorSelectionWithMenu class extends the StickyNoteColorSelection class to include color selection menus for background and drawing colors + delete menu. 
 * The class also handles synchronizing the colors modification on the network : 
 *   - The UI is updated on remote players thanks to the networked color variables (BackgroundColorMenuIndex & DrawingColorMenuIndex).
 *   - The background color is updated with "ChangeColor(backgroundColorRequestedWithMenu);"
 * 
 ***/
public class StickyNoteColorSelectionWithMenu : StickyNoteColorSelection
{    
    [Networked, OnChangedRender(nameof(OnDrawingColorMenuIndexChanged))]
    [SerializeField] private int DrawingColorMenuIndex { get; set; }

    private int backgroundColorRequestedWithMenu = -1;

    private int previousDrawingColorMenuIndex = -1;
    private int drawingColorRequestedWithMenu = 5;


    [SerializeField]
    GameObject backgroundColorRadioGroupButtonsParent;
    [SerializeField]
    List<SpatialButtonWithIcons> backgroundColorRadioGroupButtons = new List<SpatialButtonWithIcons>();

    [SerializeField]
    GameObject drawingColorRadioGroupButtonsParent;
    [SerializeField]
    List<SpatialButtonWithIcons> drawingColorRadioGroupButtons = new List<SpatialButtonWithIcons>();

    TouchDrawing touchDrawing;

    NetworkGrabbable networkGrabbable;

    [SerializeField]
    GameObject deleteMenu;
    protected override void Awake()
    {
        base.Awake();
        if (backgroundColorRadioGroupButtonsParent == null)
            Debug.LogError("radioGroupButtonsParent is not set");
        if (drawingColorRadioGroupButtonsParent == null)
            Debug.LogError("drawingColorRadioGroupButtonsParent is not set");

        if (backgroundColorRadioGroupButtonsParent)
        {
            foreach (Transform child in backgroundColorRadioGroupButtonsParent.transform)
            {
                if (child == transform) continue;
                if (child.TryGetComponent<SpatialButtonWithIcons>(out var sibling))
                {
                    if (sibling.IsRadioButton)
                        backgroundColorRadioGroupButtons.Add(sibling);
                }
            }
        }

        if (drawingColorRadioGroupButtonsParent)
        {
            foreach (Transform child in drawingColorRadioGroupButtonsParent.transform)
            {
                if (child == transform) continue;
                if (child.TryGetComponent<SpatialButtonWithIcons>(out var sibling))
                {
                    if (sibling.IsRadioButton)
                        drawingColorRadioGroupButtons.Add(sibling);
                }
            }
        }

        if (touchDrawing == null)
            touchDrawing = GetComponent<TouchDrawing>();


        if (networkGrabbable == null)
            networkGrabbable = GetComponent<NetworkGrabbable>();

        ToggleDeleteMenu();
    }
    public override void Spawned()
    {
        base.Spawned();
        OnDrawingColorMenuIndexChanged();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (Object == null || Object.HasStateAuthority == false) return;
        // Realign background color change request done during regular updates
        CheckBackgroundColorSelectionWithStickyNoteMenu();
        CheckDrawingColorSelectionWithStickyNoteMenu();
    }

    private void CheckBackgroundColorSelectionWithStickyNoteMenu()
    {
        if (backgroundColorRequestedWithMenu != -1 && colorIndex != backgroundColorRequestedWithMenu)
        {
            ChangeColor(backgroundColorRequestedWithMenu);
            backgroundColorRequestedWithMenu = -1;
        }
    }
    private void CheckDrawingColorSelectionWithStickyNoteMenu()
    {
        if (DrawingColorMenuIndex != drawingColorRequestedWithMenu)
        {
            if (Object.HasStateAuthority)
            {
                // change the DrawingColorMenuIndex networked var so remote player's UI will be updated
                DrawingColorMenuIndex = drawingColorRequestedWithMenu;
            }
        }
    }

    private void ApplyTouchDrawingColorChange(int index)
    {
        if (index >= colorList.Count || index < 0)
            Debug.LogError("Index color out of range");
        else
        {
            touchDrawing.drawingColor = colorList[index];
        }
    }

    // Update the menu selected color when the network var BackgroundColorMenuIndex has been changed

    protected override void ApplyColorChange(Color color)
    {
        base.ApplyColorChange(color);

        // update buttons
        ActivateRadioButton(backgroundColorRadioGroupButtons, colorIndex);
    }

    // Update the menu selected color when the network var DrawingColorMenuIndex has been changed
    protected virtual void OnDrawingColorMenuIndexChanged()
    {
        // change drawing color
        ApplyTouchDrawingColorChange(DrawingColorMenuIndex);

        if (DrawingColorMenuIndex == previousDrawingColorMenuIndex) return;
        // Update the color index
        previousDrawingColorMenuIndex = DrawingColorMenuIndex;

        // update buttons
        ActivateRadioButton(drawingColorRadioGroupButtons, DrawingColorMenuIndex);
    }


    // ActivateRadioButton is called to update the button status in case the color was changed by a remote user (without touching the button)
    void ActivateRadioButton(List<SpatialButtonWithIcons> spatialButtonWithIconsList, int index)
    {
        for (int i = 0; i < spatialButtonWithIconsList.Count; i++)
        {
            if (i == index)
            {
                spatialButtonWithIconsList[i].toggleStatus = true;
            }
            else
            {
                spatialButtonWithIconsList[i].toggleStatus = false;
            }
            spatialButtonWithIconsList[i].UpdateButton();
        }
    }

    // ChangeStickyNoteColor is called by menu buttons (SpatialButtonWithIcons) in the scene
    public async void ChangeStickyNoteColor(int index)
    {
        if (Object == null) return;
        await Object.WaitForStateAuthority();
        backgroundColorRequestedWithMenu = index;
    }

    // ChangeDrawingColor is called by menu buttons (SpatialButtonWithIcons) in the scene
    public async void ChangeDrawingColor(int index)
    {
        if (Object == null) return;
        await Object.WaitForStateAuthority();
        drawingColorRequestedWithMenu = index;
    }

    public void CancelDeletion()
    {
        deleteMenuIsEnabled = false;
        deleteMenu.SetActive(deleteMenuIsEnabled);   
    }

    bool deleteMenuIsEnabled = true;
    float lastToggleMenu = 0f;
    float bouncePreventionDelay = 0.3f;

    public void ToggleDeleteMenu()
    {
        if(lastToggleMenu + bouncePreventionDelay < Time.time)
        {
            lastToggleMenu = Time.time;
            deleteMenuIsEnabled = !deleteMenuIsEnabled;
            deleteMenu.SetActive(deleteMenuIsEnabled);
        }
    }

    public void ToggleDeleteMenu(bool newStatus)
    {
        if (deleteMenuIsEnabled != newStatus)
            ToggleDeleteMenu();
    }
}
