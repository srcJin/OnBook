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
        public List<BoneData> bones = new List<BoneData>();
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
        Debug.Log("Press 'P' to start playback.");
        LoadDataFromJson();

        // Cache all the bones of the avatar in order
        avatarBones = avatar.GetComponentsInChildren<Transform>();
        Debug.Log($"Avatar has {avatarBones.Length} bones.");
    }

    void Update()
    {
        // Start playback with the 'P' key
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

        // Handle playback logic
        if (isPlaying && currentFrameIndex < playbackData.Count)
        {
            float elapsedTime = Time.time - playbackStartTime;

            // Play frames in sequence based on timestamp
            while (currentFrameIndex < playbackData.Count &&
                   elapsedTime >= playbackData[currentFrameIndex].timestamp)
            {
                ApplyFrameData(playbackData[currentFrameIndex]);
                currentFrameIndex++;
            }

            // Stop playback when all frames are played
            if (currentFrameIndex >= playbackData.Count)
            {
                Debug.Log("Playback finished.");
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
        // Ensure the number of bones matches between the recorded data and the avatar
        if (avatarBones.Length != frameData.bones.Count)
        {
            Debug.LogError("Mismatch between the number of avatar bones and recorded bones.");
            return;
        }

        for (int i = 0; i < frameData.bones.Count; i++)
        {
            var boneData = frameData.bones[i];
            var boneTransform = avatarBones[i];

            // Apply position and rotation to the avatar's bone
            boneTransform.localPosition = new Vector3(boneData.position.x, boneData.position.y, boneData.position.z);
            boneTransform.localRotation = new Quaternion(boneData.rotation.x, boneData.rotation.y, boneData.rotation.z, boneData.rotation.w);
        }
    }
}
