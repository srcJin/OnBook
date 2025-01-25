using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FullBodyTrackingRecorder : MonoBehaviour
{
    public OVRBody ovrBody; // Reference to the OVRBody component in your scene
    private bool isRecording = false;
    private List<BodyFrameData> recordedData = new List<BodyFrameData>();
    private float recordingStartTime;

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

    void Start()
    {
        Debug.Log("Press 'R' to start/stop recording.");
    }

    void Update()
    {
        // Toggle recording with the 'R' key
        if (Input.GetKeyDown(KeyCode.R))
        {
            isRecording = !isRecording;

            if (isRecording)
            {
                Debug.Log("Recording started...");
                recordedData.Clear();
                recordingStartTime = Time.time; // Set the start time
            }
            else
            {
                Debug.Log("Recording stopped. Saving data...");
                SaveDataToJson();
            }
        }
    }

    void FixedUpdate()
    {
        if (isRecording)
        {
            RecordFrameData();
        }
    }

    void RecordFrameData()
    {
        if (ovrBody == null)
        {
            Debug.LogError("OVRBody component is not assigned.");
            return;
        }

        BodyFrameData frameData = new BodyFrameData
        {
            timestamp = Time.time - recordingStartTime // Elapsed time since start
        };

        // Get the tracked joints from OVRBody
        var bodyJoints = ovrBody.; // Fetch all joints (bones)
        foreach (var joint in bodyJoints)
        {
            if (joint.Transform != null)
            {
                BoneData boneData = new BoneData
                {
                    boneName = joint.Id.ToString(),
                    position = joint.Transform.position,
                    rotation = joint.Transform.rotation
                };
                frameData.bones.Add(boneData);
            }
        }

        recordedData.Add(frameData);
    }

    void SaveDataToJson()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogWarning("No data recorded. JSON file will be empty.");
            return;
        }

        // Convert recorded data to JSON
        string json = JsonUtility.ToJson(new Wrapper<BodyFrameData> { frames = recordedData }, true);

        // Save JSON to local storage
        string filePath = Path.Combine(Application.persistentDataPath, "body_tracking_data.json");
        File.WriteAllText(filePath, json);

        Debug.Log($"Data saved to {filePath}");
    }

    // Wrapper class for JSON serialization
    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> frames;
    }
}
