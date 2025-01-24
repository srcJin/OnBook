using Fusion.Addons.VisionOsHelpers;
using UnityEngine;

/***
 * 
 * The SpatialButtonWithIcons class extends the SpatialButton component to include support for displaying primary and secondary icons based on button state. 
 * It updates the icons based on the button type (press button, toggle button, or radio button) and the current button status. 
 * The class also handles material changes for the button mesh renderer when the button is pressed or released. 
 * 
 ***/
public class SpatialButtonWithIcons : SpatialButton
{

    [SerializeField] GameObject primaryIcon;
    [SerializeField] GameObject secondaryIcon;
    public override void UpdateButton()
    {
        base.UpdateButton();

          if(buttonType == ButtonType.PressButton)
          {
              if (primaryIcon != null)
              {
                  primaryIcon.SetActive(true);

                  if (secondaryIcon != null)
                  {
                      primaryIcon.SetActive(!isButtonPressed);
                      secondaryIcon.SetActive(isButtonPressed);
                  }
              }
          }

          if(IsToggleButton || IsRadioButton)
          {
              if (primaryIcon != null)
              {
                  primaryIcon.SetActive(toggleStatus);

                  if (secondaryIcon != null)
                      secondaryIcon.SetActive(!toggleStatus);

              }
          }
    }
}
