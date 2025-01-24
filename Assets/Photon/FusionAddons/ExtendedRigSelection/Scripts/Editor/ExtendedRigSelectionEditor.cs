using Fusion.Addons.ExtendedRigSelectionAddon;
using UnityEditor;
using UnityEngine;


/***
 * 
 * ExtendedRigSelectionEditor adds editor buttons to choose between a list of rigs. There are 3 kings of buttons :
 * - buttons that save the choice in user preference
 * - buttons that force the rig selection
 * - a single button which displays an UI to select the rig at runtime
 * 
 ***/
[CustomEditor(typeof(ExtendedRigSelection))]
public class ExtendedRigSelectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ExtendedRigSelection rigSelection = (ExtendedRigSelection)target;

        var currentRigDescription = rigSelection.PreferedRigDescription();

        // Create Rig buttons with preference selection
        foreach (var rigDescription in rigSelection.rigKindDescriptions)
        {
            var selected = currentRigDescription != null && currentRigDescription.GetValueOrDefault().name == rigDescription.name;
            var selectedText = selected ? " (current pref.)" : "";
            var currentMode = selected && rigSelection.selectionMode == ExtendedRigSelection.SelectionMode.SelectedByUserPref;
            var currentModeText = currentMode?"> ":"";
            if (GUILayout.Button($"{currentModeText}Use {rigDescription.name} Rig with preference selection{selectedText}"))
            {
                rigSelection.SavePreference(rigDescription);
                var mode = serializedObject.FindProperty("selectionMode");
                mode.intValue = (int)ExtendedRigSelection.SelectionMode.SelectedByUserPref;
                serializedObject.ApplyModifiedProperties();
            }
        }

        // Create UI selection Button
        var currentModeUI = rigSelection.selectionMode == ExtendedRigSelection.SelectionMode.SelectedByUI;
        var currentModeUIText = currentModeUI ? "> " : "";
        if (GUILayout.Button($"{currentModeUIText}Use UI selection"))
        {
            var mode = serializedObject.FindProperty("selectionMode");
            mode.intValue = (int)ExtendedRigSelection.SelectionMode.SelectedByUI;
            serializedObject.ApplyModifiedProperties();
        }

        // Create Force Rig buttons
        foreach (var rigDescription in rigSelection.rigKindDescriptions)
        {
            var selected = rigSelection.forcedKindName == rigDescription.name;
            var selectedText = selected ? " (current)" : "";
            var currentMode = selected && rigSelection.selectionMode == ExtendedRigSelection.SelectionMode.SelectedByForcedValue;
            var currentModeText = currentMode ? "> " : "";
            if (GUILayout.Button($"{currentModeText}Force to use {rigDescription.name} {selectedText}"))
            {
                var forcedKindName = serializedObject.FindProperty("forcedKindName");
                forcedKindName.stringValue = rigDescription.name;

                var mode = serializedObject.FindProperty("selectionMode");
                mode.intValue = (int)ExtendedRigSelection.SelectionMode.SelectedByForcedValue;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
