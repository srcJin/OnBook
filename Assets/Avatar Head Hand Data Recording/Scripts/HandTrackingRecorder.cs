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
        Debug.Log("Press 'R' to start/stop recording.");
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
            foreach (var bone in leftSkeleton.Bones)
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

        // Record right hand bones
        if (rightHand.IsTracked)
        {
            foreach (var bone in rightSkeleton.Bones)
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

        recordedData.Add(handData);
    }

    void SaveDataToJson()
    {
        string json = JsonUtility.ToJson(new { handsData = recordedData }, true); // Wrap data for clean JSON
        string filePath = Path.Combine(Application.persistentDataPath, "hand_data.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Data saved to {filePath}");
    }
}
