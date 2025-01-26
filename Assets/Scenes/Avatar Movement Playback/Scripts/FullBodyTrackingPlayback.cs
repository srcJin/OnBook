using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FullBodyTrackingPlayback : MonoBehaviour
{
    public GameObject avatar; // Reference to the avatar
    public string jsonFileName = "body_tracking_data.json"; // Name of the JSON file to load
    private List<BodyFrameData> playbackData = new List<BodyFrameData>();
    private bool isPlaying = false;
    private float playbackStartTime;
    private int currentFrameIndex = 0;

    [System.Serializable]
    public class BodyFrameData
    {
        public float timestamp; // Time since recording started
        public List<BoneData> humanoidBones = new List<BoneData>();
    }

    [System.Serializable]
    public class BoneData
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> frames;
    }

    private Transform[] avatarBones;

    void Start()
    {
        Debug.Log("Initializing FullBodyTrackingPlayback...");

        LoadDataFromJson();

        // Cache all the bones of the avatar in order
        avatarBones = avatar.GetComponentsInChildren<Transform>();
        Debug.Log($"Avatar initialization complete. Found {avatarBones.Length} bones.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isPlaying)
            {
                Debug.Log("Playback started...");
                playbackStartTime = Time.time; // Set playback start time
                currentFrameIndex = 0; // Reset to the first frame
                isPlaying = true;
            }
        }

        if (isPlaying && currentFrameIndex < playbackData.Count)
        {
            float elapsedTime = Time.time - playbackStartTime;

            while (currentFrameIndex < playbackData.Count &&
                   elapsedTime >= playbackData[currentFrameIndex].timestamp)
            {
                Debug.Log($"Applying frame {currentFrameIndex + 1}/{playbackData.Count} at timestamp {elapsedTime:F2}s");
                ApplyFrameData(playbackData[currentFrameIndex]);
                currentFrameIndex++;
            }

            if (currentFrameIndex >= playbackData.Count)
            {
                Debug.Log("Playback finished. All frames have been applied.");
                isPlaying = false;
            }
        }
    }

    void LoadDataFromJson()
    {
        string filePath = Path.Combine(Application.persistentDataPath, jsonFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"JSON file not found at {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        Wrapper<BodyFrameData> wrapper = JsonUtility.FromJson<Wrapper<BodyFrameData>>(json);
        playbackData = wrapper.frames;

        Debug.Log($"Loaded {playbackData.Count} frames of playback data from {filePath}");
    }

    void ApplyFrameData(BodyFrameData frameData)
    {
        if (avatarBones.Length == 0)
        {
            Debug.LogError("No avatar bones found. Ensure the avatar is correctly configured.");
            return;
        }

        Debug.Log($"Processing frame at timestamp {frameData.timestamp:F2}s with {frameData.humanoidBones.Count} bones.");

        foreach (var humanoidBoneData in frameData.humanoidBones)
        {
            string cleanBoneName = humanoidBoneData.boneName.Replace("_", "");
            Debug.Log($"Attempting to map bone: Original='{humanoidBoneData.boneName}', Clean='{cleanBoneName}'");

            bool boneFound = false;

            foreach (HumanBodyBones humanBone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                string humanBoneName = humanBone.ToString();
                if (cleanBoneName.Equals(humanBoneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    boneFound = true;
                    Debug.Log($"Found Bone {humanBoneName}");
                    Transform boneTransform = avatar.GetComponent<Animator>().GetBoneTransform(humanBone);
                    if (boneTransform != null)
                    {
                        Debug.Log($"Applying transformation to bone: {humanBoneName}");
                        boneTransform.localPosition = humanoidBoneData.position;
                        boneTransform.localRotation = humanoidBoneData.rotation;
                    }
                    else
                    {
                        Debug.LogWarning($"Bone '{humanBoneName}' not found on the avatar's Animator.");
                    }
                    break;
                }
            }

            if (!boneFound)
            {
                Debug.LogWarning($"Bone '{humanoidBoneData.boneName}' (cleaned: '{cleanBoneName}') did not match any HumanBodyBones.");
            }
        }
    }
}
