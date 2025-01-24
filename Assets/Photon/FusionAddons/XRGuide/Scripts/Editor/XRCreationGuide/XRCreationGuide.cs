using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class XRCreationGuide : EditorWindow, XRActionsManager.IListener
{
    public static XRCreationGuide SharedInstance;

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    public VisualElement runnerViewcontainer;
    public VisualElement hardwareRigViewcontainer;
    public VisualElement networkRigViewcontainer;
    public VisualElement sceneObjectsViewcontainer;

    VisualTreeAsset buttonTemplate;

    [MenuItem("Window/Fusion/XR/XRCreationGuide")]
    public static void ShowXRCreationGuide()
    {
        XRCreationGuide wnd = GetWindow<XRCreationGuide>();
        wnd.titleContent = new GUIContent("XRCreationGuide");
    }

    private void OnDestroy()
    {
        XRActionsManager.UnregisterListener(this);
    }

    public static bool TryFindAssetByName<T>(string assetName, out string path)
    {
        path = null;
        var assets = AssetDatabase.FindAssets(assetName);
        foreach (var a in assets)
        {
            path = AssetDatabase.GUIDToAssetPath(a);
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(T))
            {
                return true;
            }
        }
        return false;
    }

    public static T LoadAsset<T>(string name) where T:UnityEngine.Object{
        if (TryFindAssetByName<T>(name, out var path))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset == null)
            {
                throw new Exception($"Asset at {path} is of the expected type. Type: "+AssetDatabase.GetMainAssetTypeAtPath(path));
            }
            return asset;
        }
        else
        {
            throw new Exception("Unable to find asset "+name);
        }
    }

    void EnableToggleButton(VisualElement categoryView)
    {
        var toggleViewbutton = categoryView.Q<Button>("toggle-view-button");
        var scrollView = categoryView.Q<ScrollView>("button-scrollview");
        if (toggleViewbutton != null && scrollView != null)
        {
            toggleViewbutton.clicked += () => {
                scrollView.style.display = scrollView.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
                toggleViewbutton.text = scrollView.style.display == DisplayStyle.None ? "+" : "-";
            };
        }
    }

    public void CreateGUI()
    {
        SharedInstance = this;
        EditorSceneManager.activeSceneChanged += OnActiveSceneChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;

        XRActionsManager.RegisterListener(this);
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        buttonTemplate = LoadAsset<VisualTreeAsset>("XRCreationGuideButton");

        // Instantiate UXML
        VisualElement contentFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(contentFromUXML);

        var categoryView = contentFromUXML.Q<VisualElement>("runner-view");
        runnerViewcontainer = categoryView.Q<VisualElement>("unity-content-container");
        EnableToggleButton(categoryView);
        categoryView = contentFromUXML.Q<VisualElement>("hardwarerig-view");
        hardwareRigViewcontainer = categoryView.Q<VisualElement>("unity-content-container");
        EnableToggleButton(categoryView);
        categoryView = contentFromUXML.Q<VisualElement>("networkrig-view");
        networkRigViewcontainer = categoryView.Q<VisualElement>("unity-content-container");
        EnableToggleButton(categoryView);

        categoryView = contentFromUXML.Q<VisualElement>("sceneobjects-view");
        sceneObjectsViewcontainer = categoryView.Q<VisualElement>("unity-content-container");
        EnableToggleButton(categoryView);
        




        runnerViewcontainer.Clear();
        hardwareRigViewcontainer.Clear();
        networkRigViewcontainer.Clear();
        sceneObjectsViewcontainer.Clear();
        UpdateActions();
    }

    private void OnHierarchyChanged()
    {
        UpdateActions();
    }

    private void OnActiveSceneChanged(Scene arg0, Scene arg1)
    {
        UpdateActions();
    }

    public void OnActionsChange(IXRAction action)
    {
        Debug.LogError("Actions changed");
        UpdateActions();
    }

    Button ButtonFor(IXRAction action, VisualElement container)
    {
        Button button;
        if (buttonForActions.ContainsKey(action))
        {
            button = buttonForActions[action];
        }
        else
        {
            button = buttonTemplate.Instantiate().Q<Button>("guide-button");
            button.AddToClassList("xr-button");
            button.clicked += () => {
                action.Trigger();
                XRCreationGuide.SharedInstance.UpdateActions();
            };
            
            buttonForActions[action] = button;
            container.Add(button);
        }
        return button;
    }

    void UpdateActionButton(IXRAction action, VisualElement container, IXRRootAction rootAction)
    {
        bool isRootActionOk = action is IXRRootAction || (rootAction != null && rootAction.IsInstalled) || rootAction == null;
        var button = ButtonFor(action, container);
        var label = button.Q<Label>("guide-button-title");
        var image = button.Q<VisualElement>("guide-button-image");
        button.style.display = (isRootActionOk && action.IsActionVisible) ? DisplayStyle.Flex : DisplayStyle.None;
        label.text = action.Description.ToUpper();
        button.tooltip = System.Text.RegularExpressions.Regex.Replace(action.Description, "<.*?>", "");
        image.style.backgroundImage = null;
        var imageName = action.ImageName;
        if (string.IsNullOrEmpty(imageName) == false)
        {
            try
            {
                image.style.backgroundImage = LoadAsset<Texture2D>(imageName);
            }
            catch (Exception e)
            {
                Debug.LogError("Unable to find editor image " + imageName + "\n" + e);
            }
        }
    }

    Dictionary<IXRAction, Button> buttonForActions = new Dictionary<IXRAction, Button>();
    public void UpdateActions()
    {
        var runnerRootAction = XRActionsManager.RootActionForCategory(XRActionsManager.RUNNER_CATEGORY);
        var hardwareRigRootAction = XRActionsManager.RootActionForCategory(XRActionsManager.HARDWARERIG_CATEGORY);
        var networkRigRootAction = XRActionsManager.RootActionForCategory(XRActionsManager.NETWORKRIG_CATEGORY);
        var sceneObjectsRootAction = XRActionsManager.RootActionForCategory(XRActionsManager.SCENE_OBJECT_CATEGORY);

        foreach (var action in XRActionsManager.ActionsInCategory(XRActionsManager.RUNNER_CATEGORY))
        {
            UpdateActionButton(action, runnerViewcontainer, runnerRootAction);
        }
        foreach (var action in XRActionsManager.ActionsInCategory(XRActionsManager.HARDWARERIG_CATEGORY))
        {
            UpdateActionButton(action, hardwareRigViewcontainer, hardwareRigRootAction);
        }
        foreach (var action in XRActionsManager.ActionsInCategory(XRActionsManager.NETWORKRIG_CATEGORY))
        {
            UpdateActionButton(action, networkRigViewcontainer, networkRigRootAction);
        }
        foreach (var action in XRActionsManager.ActionsInCategory(XRActionsManager.SCENE_OBJECT_CATEGORY))
        {
            UpdateActionButton(action, sceneObjectsViewcontainer, sceneObjectsRootAction);
        }
    }
}
