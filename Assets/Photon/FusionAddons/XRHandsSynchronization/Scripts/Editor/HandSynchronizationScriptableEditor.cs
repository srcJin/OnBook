using UnityEditor;
using UnityEngine;

using Fusion.Addons.HandsSync;


[CustomEditor(typeof(HandSynchronizationScriptable))]
public class XRHandSynchronizationScriptableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HandSynchronizationScriptable scriptable = (HandSynchronizationScriptable)target;
        if (GUILayout.Button($"Storage byte size required: {scriptable.BoneInfoByteSize}\n(click to refresh total byte size)", GUILayout.Height(40f)))
        {
            scriptable.ResetBoneInfo();
            scriptable.UpdateBonesInfo();
        }
        base.OnInspectorGUI();
    }
}
