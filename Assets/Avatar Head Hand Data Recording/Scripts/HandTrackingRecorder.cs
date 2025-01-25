using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HandTrackingRecorder : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    private bool isRecording = false;
    private List<HandData> recordedData = new List<HandData>();
    private float recordingStartTime; // Track when recording starts

    // Struct for hand data
    [System.Serializable]
    public class HandData
    {
        public float time; // Elapsed recording time in seconds
        public List<BoneData> leftHandBones = new List<BoneData>();
        public List<BoneData> rightHandBones = new List<BoneData>();
    }

    // Struct for individual bone data
    [System.Serializable]
    public class BoneData
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        Debug.Log("Press 'space' to start/stop recording.");
    }

    void Update()
    {
        // Toggle recording on/off with the 'space' key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isRecording = !isRecording;

            if (isRecording)
            {
                Debug.Log("Recording started...");
                recordedData.Clear(); // Clear any previous data
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
            RecordHandData();
        }
    }

    void RecordHandData()
    {
        HandData handData = new HandData();
        handData.time = Time.time - recordingStartTime; // Calculate elapsed time

        // Record left hand bones
        if (leftHand.IsTracked)
        {
            foreach (var bone in leftSkeleton.Bones)
            {
                if (bone.Transform != null)
                {
                    BoneData boneData = new BoneData
                    {
                        boneName = bone.Id.ToString(),
                        position = bone.Transform.position,
                        rotation = bone.Transform.rotation
                    };
                    handData.leftHandBones.Add(boneData);
                }
            }
        }

        // Record right hand bones
        if (rightHand.IsTracked)
        {
            foreach (var bone in rightSkeleton.Bones)
            {
                if (bone.Transform != null)
                {
                    BoneData boneData = new BoneData
                    {
                        boneName = bone.Id.ToString(),
                        position = bone.Transform.position,
                        rotation = bone.Transform.rotation
                    };
                    handData.rightHandBones.Add(boneData);
                }
            }
        }

        // Add data only if at least one hand has bone data
        if (handData.leftHandBones.Count > 0 || handData.rightHandBones.Count > 0)
        {
            recordedData.Add(handData);
        }
    }

    void SaveDataToJson()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogWarning("No data recorded. JSON file will be empty.");
            return;
        }

        Debug.Log($"Recording {recordedData.Count} frames of hand data.");

        string json = JsonUtility.ToJson(new Wrapper<HandData> { handsData = recordedData }, true);
        string filePath = Path.Combine(Application.persistentDataPath, "hand_data.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Data saved to {filePath}");
    }

    // Wrapper class for serialization
    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> handsData;
    }
}
