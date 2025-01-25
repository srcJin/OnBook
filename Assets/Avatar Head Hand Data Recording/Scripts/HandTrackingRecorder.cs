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

    // Struct for hand data
    [System.Serializable]
    public class HandData
    {
        public float time; // Timestamp
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
            //Debug.Log("FixedUpdate: Recording data...");
            RecordHandData();
        }
    }

    void RecordHandData()
    {
        HandData handData = new HandData();
        handData.time = Time.time;

        // Record left hand bones
        if (leftHand.IsTracked)
        {
            Debug.Log("Left hand is tracked.");
            foreach (var bone in leftSkeleton.Bones)
            {
                if (bone.Transform != null)
                {
                    Debug.Log($"Left Bone: {bone.Id} Position: {bone.Transform.position}");
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
        else
        {
            Debug.LogWarning("Left hand is not tracked.");
        }

        // Record right hand bones
        if (rightHand.IsTracked)
        {
            Debug.Log("Right hand is tracked.");
            foreach (var bone in rightSkeleton.Bones)
            {
                if (bone.Transform != null)
                {
                    Debug.Log($"Right Bone: {bone.Id} Position: {bone.Transform.position}");
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
        else
        {
            Debug.LogWarning("Right hand is not tracked.");
        }

        // Add data only if at least one hand has bone data
        if (handData.leftHandBones.Count > 0 || handData.rightHandBones.Count > 0)
        {
            recordedData.Add(handData);
        }
        else
        {
            Debug.LogWarning("No bones recorded for either hand.");
        }
    }


    void SaveDataToJson()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogWarning("No data recorded. JSON file will be empty.");
            return;
        }

        // Debugging recorded data count
        Debug.Log($"Recording {recordedData.Count} frames of hand data.");

        // Convert recordedData to JSON
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
