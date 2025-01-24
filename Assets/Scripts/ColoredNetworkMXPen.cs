using Fusion;
using Fusion.Addons.MXPenIntegration;
using Fusion.Addons.TextureDrawing;
using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;


/***
 * 
 *  ColoredNetworkMXPen is in charged to change the drawing color when the user uses the Logitech MX Ink hardware button or interact with a color button (ColorDipper)
 *  CheckColorModification() method is called during FUN to check if the local user used the button to change the color.
 *          
 ***/
public class ColoredNetworkMXPen : NetworkMXPen, IColorProvider
{
    [Networked, OnChangedRender(nameof(OnColorChanged))]
    public Color CurrentColor { get; set; }
      
    private Color previousColor = Color.clear;
    public List<Color> colorList = new List<Color> {
            new Color(10f/255f, 10f/255f, 10f/255f, 1),
            new Color(255f/255f, 100f/255f, 100f/255f, 1),
            new Color(121f/255f, 255f/255f, 86f/255f, 1),
            new Color(45f/255f, 156f/255f, 255f/255f, 1),
            new Color(255f/255f, 246f/255f, 76f/255f, 1)
        };

    protected int colorIndex = 0;
    [SerializeField] private float changeColorCoolDown = 1f;
    private float lastColorChangedTime = 0f;

    TexturePen texturePen;


    [SerializeField] private List<Renderer> coloredRenderers = new List<Renderer>();

    [Header("Color Change Feedback")]
    [SerializeField] string colorChangeAudioType;
    [SerializeField] FeedbackMode colorChangeFeedbackMode = FeedbackMode.AudioAndHaptic;

    protected virtual void OnColorChanged()
    {
        if (CurrentColor == previousColor) return;
        // Update the color
        previousColor = CurrentColor;
        ApplyColorChange(CurrentColor);
    }

    protected virtual void ApplyColorChange(Color color)
    {
        // The NetworkLineDrawer and the TexturePen will use the color as we are implementing IColorProvider
        foreach (var r in coloredRenderers) r.material.color = color;
    }

#if OCULUS_SDK_AVAILABLE
    protected override void Awake()
    {
        base.Awake();
        texturePen = GetComponent<TexturePen>();
    }

    public override void Spawned()
    {
        base.Spawned();
        // Set the default color
        if (Object.HasStateAuthority)
        {
            CurrentColor = DefaultColor();
        }
        OnColorChanged();
    }

    private void ChangeColor()
    {
        // check color index
        if (colorIndex >= colorList.Count)
            colorIndex = 0;
        if (colorIndex < 0)
            colorIndex = colorList.Count - 1;

        // change the networked color to inform remote players and update the local color
        CurrentColor = colorList[colorIndex];
    }

    public void ChangeColor(int index)
    {
        if (index >= colorList.Count || index < 0)
            Debug.LogError("Index color out of range");
        else
        {
            colorIndex = index;
            ChangeColor();
        }
    }

    protected Color DefaultColor()
    {
        return colorList[0];
    }


    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (Object == null || Object.HasStateAuthority == false) return;

        CheckColorModification();
    }

    public override void Render()
    {
        base.Render();
    }

    protected virtual bool IsColorChangeRequested()
    {
        if (localHardwareStylus)
            return localHardwareStylus.CurrentState.cluster_front_value;
        else
            return false;
    }

    private void CheckColorModification()
    {
        // Check if the the local player press the color modification button
        if (lastColorChangedTime + changeColorCoolDown < Time.time)
        {
            if (IsColorChangeRequested())
            {
                // button has been used, change the color index
                lastColorChangedTime = Time.time;

                colorIndex++;

                // Apply color update
                ChangeColor();

                // Haptic feedback
                if (feedback != null)
                {
                    feedback.PlayAudioAndHapticFeeback(audioType: colorChangeAudioType, audioOverwrite: true, feedbackMode: colorChangeFeedbackMode);
                }
            }
        }
    }

    protected override bool ShouldStopCurrentVolumeDrawing()
    {
        return localHardwareStylus.CurrentState.cluster_back_value;
    }
#endif
}
