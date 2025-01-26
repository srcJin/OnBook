using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class Recorder2 : MonoBehaviour
{
    public OVRBody ovrBody; // Reference to the OVRBody component
    public GameObject armatureSkinningUpdateTPose; // Drag and drop the GameObject here

    private bool isRecording = false;
    private List<BodyFrameData> recordedData = new List<BodyFrameData>();
    private float recordingStartTime;

    [System.Serializable]
    public class BodyFrameData
    {
        public float timestamp;
        public List<BoneData> bones = new List<BoneData>();
        public List<HumanoidBoneData> humanoidBones = new List<HumanoidBoneData>();
    }

    [System.Serializable]
    public class BoneData
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class HumanoidBoneData
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        Debug.Log("Press 'Space' to start/stop recording.");
        DebugHierarchy(armatureSkinningUpdateTPose.transform);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isRecording = !isRecording;

            if (isRecording)
            {
                Debug.Log("Recording started...");
                recordedData.Clear();
                recordingStartTime = Time.time;
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
        BodyFrameData frameData = new BodyFrameData
        {
            timestamp = Time.time - recordingStartTime
        };

        // Record OVRBody data
        if (ovrBody != null && ovrBody.BodyState.HasValue)
        {
            var jointLocations = ovrBody.BodyState.Value.JointLocations;
            for (int i = 0; i < jointLocations.Length; i++)
            {
                var joint = jointLocations[i];
                if (joint.PositionValid || joint.OrientationValid)
                {
                    string boneName = NormalizeBoneName(((OVRPlugin.BoneId)i).ToString());
                    Transform jointTransform = armatureSkinningUpdateTPose.transform.Find(boneName);

                    if (jointTransform != null)
                    {
                        BoneData boneData = new BoneData
                        {
                            boneName = boneName,
                            position = jointTransform.localPosition, // Record local position
                            rotation = jointTransform.localRotation  // Record local rotation
                        };
                        frameData.bones.Add(boneData);
                    }
                    else
                    {
                        Debug.LogWarning($"Joint '{boneName}' not found in the hierarchy.");
                    }
                }
            }
        }

        // Record Humanoid bone data
        if (armatureSkinningUpdateTPose != null)
        {
            var retargeter = armatureSkinningUpdateTPose.GetComponent<OVRUnityHumanoidSkeletonRetargeter>();
            if (retargeter != null)
            {
                var animator = retargeter.GetComponent<Animator>();
                if (animator != null)
                {
                    for (HumanBodyBones hb = HumanBodyBones.Hips; hb < HumanBodyBones.LastBone; hb++)
                    {
                        Transform boneTransform = animator.GetBoneTransform(hb);
                        if (boneTransform != null)
                        {
                            HumanoidBoneData hBoneData = new HumanoidBoneData
                            {
                                boneName = hb.ToString(),
                                position = boneTransform.localPosition, // Record local position
                                rotation = boneTransform.localRotation  // Record local rotation
                            };
                            frameData.humanoidBones.Add(hBoneData);
                        }
                        else
                        {
                            Debug.LogWarning($"Humanoid bone '{hb}' not found in the hierarchy.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Animator not found on retargeter object.");
                }
            }
            else
            {
                Debug.LogWarning("OVRUnityHumanoidSkeletonRetargeter not found on ArmatureSkinningUpdateTPose.");
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

        string json = JsonUtility.ToJson(new Wrapper<BodyFrameData> { frames = recordedData }, true);
        string filePath = Path.Combine(Application.persistentDataPath, "body_tracking_data.json");
        File.WriteAllText(filePath, json);

        Debug.Log($"Data saved to {filePath}");
    }

    void DebugHierarchy(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Debug.Log($"Found joint: {child.name}");
            DebugHierarchy(child); // Recursively log child joints
        }
    }

    string NormalizeBoneName(string boneName)
    {
        return boneName.Replace("_", ""); // Adjust normalization logic if needed
    }

    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> frames;
    }
}
