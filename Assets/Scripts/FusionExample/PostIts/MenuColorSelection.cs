using Fusion.Addons.VisionOsHelpers;
using System.Collections.Generic;
using UnityEngine;


/***
*
* The MenuColorSelection class is responsible for managing the sitcky note color selection menu.
* It includes support for detecting menu tabs, activating/deactivating the menu, and toggling the menu on/off.
* The class also handles initializing the initial tab and preventing rapid menu toggling.
* 
***/
public class MenuColorSelection : MonoBehaviour
{
    [SerializeField]
    GameObject menuGameObject = null;

    [SerializeField]
    GameObject tabsRoot = null;

    [SerializeField]
    SpatialButton initialTabButton;

    [SerializeField]
    List<GameObject> menuTabs = new List<GameObject>();

    [SerializeField]
    GameObject initialTab = null;


    private bool menuIsEnabled = false;
    private void Start()
    {
        if (menuGameObject == null) menuGameObject = gameObject;
        if (tabsRoot == null) tabsRoot = menuGameObject;
        if (menuTabs.Count == 0)
        {
            // Detecting tabs
            foreach (Transform child in tabsRoot.transform)
            {
                if (child.GetComponentInChildren<SpatialTouchable>() != null)
                {
                    menuTabs.Add(child.gameObject);
                }
            }
        }

        if (initialTabButton)
        {
            initialTabButton.ChangeButtonStatus(true);
        }
        else
        {
            if (initialTab == null && menuTabs.Count > 0) initialTab = menuTabs[0];
        }
        if (initialTab)
        {
            ActivateMenuTab(initialTab);
        }

        menuGameObject.SetActive(menuIsEnabled);
    }


    float lastToggleMenu = 0f;
    float bouncePreventionDelay = 0.3f;
    public void ToggleColorSelectionMenu()
    {
        if (lastToggleMenu + bouncePreventionDelay < Time.time)
        {
            lastToggleMenu = Time.time;
            menuIsEnabled = !menuIsEnabled;
            menuGameObject.SetActive(menuIsEnabled);
        }
    }

    public void ToggleColorSelectionMenu(bool newStatus)
    {
        if (menuIsEnabled != newStatus)
            ToggleColorSelectionMenu();
    }

    public void ActivateMenuTab(GameObject tab)
    {
        foreach (var menuTab in menuTabs)
        {
            bool isSelectedTab = menuTab == tab;
            menuTab.SetActive(isSelectedTab);

            if (isSelectedTab)
            {
                SpatialButton defaultSpatialButton = null;
                bool shouldActivateDefaultButton = true;
                foreach (var button in menuTab.GetComponentsInChildren<SpatialButton>())
                {
                    if (button.IsRadioButton == false)
                    {
                        continue;
                    }

                    if (button.IsRadioButton)
                    {
                        if (button.isRadioGroupDefaultButton)
                        {
                            defaultSpatialButton = button;
                        }
                    }
                    else
                    {
                        if (defaultSpatialButton == null)
                            defaultSpatialButton = button;
                    }

                    if (button.toggleStatus)
                    {   // no need to select default button if a button is activated
                        shouldActivateDefaultButton = false;
                        break;
                    }
                }

                if (shouldActivateDefaultButton && defaultSpatialButton)
                {
                    defaultSpatialButton.ChangeButtonStatus(true);
                }
            }
        }
    }


}
