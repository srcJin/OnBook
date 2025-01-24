using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;
using Fusion.XR.Shared.Editor;

public class XRCreationLogger : EditorWindow, XRActionsManager.ILogListener
{
    VisualTreeAsset entryTemplate;
    ListView listview;

    StyleColor defaultEntryBackgroundColor;
    StyleColor latestAdditionStyleColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.6f));
    public const string HIGHLIGHT_COLOR = "#ceff00";
    public const string ALERT_COLOR = "#f87099";

    [MenuItem("Window/Fusion/XR/XRCreationLogger")]
    public static void ShowXRCreationLogger()
    {
        XRCreationLogger wnd = GetWindow<XRCreationLogger>();
        wnd.titleContent = new GUIContent("XRCreationLogger");
    }


    private void OnDestroy()
    {
        XRActionsManager.UnregisterLogListener(this);
    }

    public void CreateGUI()
    {
        XRActionsManager.RegisterLogListener(this);

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        var loggerTree = XRCreationGuide.LoadAsset<VisualTreeAsset>("XRCreationLogger");
        entryTemplate = XRCreationGuide.LoadAsset<VisualTreeAsset>("XRCreationLogEntry");

        // Instantiate UXML
        VisualElement contentFromUXML = loggerTree.Instantiate();

        var clearButton = contentFromUXML.Q<Button>("clear-logs");
        clearButton.clicked += () => {
            XRActionsManager.ClearLogs();
        };

        listview = contentFromUXML.Q<ListView>("log-listview");

        listview.makeItem = () => {
            var listEntry = entryTemplate.Instantiate();
            defaultEntryBackgroundColor = listEntry.style.backgroundColor;
            return listEntry;
        };
        listview.bindItem = (listEntry, index) => {
            var log = XRActionsManager.Logs[index];
            var logBackground = listEntry.Q<VisualElement>("log-entry-background");
            if (logBackground == null) logBackground = listEntry;
            if (logBackground != null)
            {
                logBackground.style.backgroundColor = log.isLatestAddition ? latestAdditionStyleColor : defaultEntryBackgroundColor;
            }
            var logLabel = listEntry.Q<Label>("log-text");
            var logImage = listEntry.Q<VisualElement>("log-image");
            var logImageContainer = listEntry.Q<VisualElement>("log-image-container");
            var logText = log.text;
            logText = logText.Replace("HIGHLIGHT_COLOR", HIGHLIGHT_COLOR);
            logText = logText.Replace("ALERT_COLOR", ALERT_COLOR);
            logLabel.text = logText;
            logLabel.tooltip = logLabel.text;
#if UNITY_2022_1_OR_NEWER
            logLabel.selection.isSelectable = true;
#endif
            logImage.style.backgroundImage = null;
            logImageContainer.style.display = DisplayStyle.None;
            var imageName = log.imageName;
            if (string.IsNullOrEmpty(imageName) == false)
            {
                try
                {
                    logImage.style.backgroundImage = XRCreationGuide.LoadAsset<Texture2D>(imageName);
                    logImageContainer.style.display = DisplayStyle.Flex;
                }
                catch (Exception e)
                {
                    Debug.LogError("Unable to find editor image " + imageName + "\n" + e);
                }
            }
        };
        listview.fixedItemHeight = 60;
        listview.selectionChanged += (IEnumerable<object> selectedItems) => {
            foreach (var i in selectedItems)
            {
                if (i is XRActionsManager.XRActionsLogEntry logEntry)
                {
                    if (logEntry.forceExitPrefabMode)
                    {
                        XRProjectAutomation.ExitPrefabMode();
                    }
                    if (logEntry.associatedObject)
                    {
                        Selection.activeObject = logEntry.associatedObject;
                    }
                    else if (logEntry.selecter != null)
                    {
                        logEntry.selecter.TrySelect();
                    }
                }
            }
            listview.ClearSelection();
        };
        listview.itemsSource = XRActionsManager.Logs;
        root.Add(contentFromUXML);
    }

    public void OnLogsChange()
    {
        listview.Rebuild();
        if(listview.itemsSource.Count > 0)
        {
            listview.ScrollToItem(listview.itemsSource.Count - 1);
#if UNITY_2022
            scrollNeeded = 2;
#endif
        }
    }

#if UNITY_2022
    int scrollNeeded = 0;
#endif 

    private void Update()
    {
#if UNITY_2022
        if (scrollNeeded > 0)
        {
            scrollNeeded--;
            // https://issuetracker.unity3d.com/issues/scrolltoitem-should-work-on-the-same-frame-the-layout-size-is-updated
            listview.ScrollToItem(listview.itemsSource.Count - 1);
        }
#endif     
    }
}