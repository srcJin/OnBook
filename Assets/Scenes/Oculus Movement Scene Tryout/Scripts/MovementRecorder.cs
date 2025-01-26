using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Records movement data for OVRBody and humanoid bones.
/// </summary>
public class MovementRecorder : MonoBehaviour
{
    public OVRBody ovrBody; // Reference to the OVRBody component
    public GameObject armatureSkinningUpdateTPose; // GameObject containing the humanoid skeleton

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

    /// <summary>
    /// Starts the recording process.
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Recording is already in progress.");
            return;
        }

        Debug.Log("Starting movement recording...");
        recordedData.Clear();
        recordingStartTime = Time.time;
        isRecording = true;
    }

    /// <summary>
    /// Stops the recording process and saves the data to a JSON file.
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Recording is not currently active.");
            return;
        }

        Debug.Log("Stopping movement recording...");
        isRecording = false;
        SaveDataToJson();
    }

    private void FixedUpdate()
    {
        if (isRecording)
        {
            RecordFrameData();
        }
    }

    private void RecordFrameData()
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
                            position = jointTransform.localPosition,
                            rotation = jointTransform.localRotation
                        };
                        frameData.bones.Add(boneData);
                    }
                }
            }
        }

        // Record humanoid bone data
        if (armatureSkinningUpdateTPose != null)
        {
            var animator = armatureSkinningUpdateTPose.GetComponent<Animator>();
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
                            position = boneTransform.localPosition,
                            rotation = boneTransform.localRotation
                        };
                        frameData.humanoidBones.Add(hBoneData);
                    }
                }
            }
        }

        recordedData.Add(frameData);
    }

    private void SaveDataToJson()
    {
        if (recordedData.Count == 0)
        {
            Debug.LogWarning("No data recorded. JSON file will be empty.");
            return;
        }

        string json = JsonUtility.ToJson(new Wrapper<BodyFrameData> { frames = recordedData }, true);
        string filePath = Path.Combine(Application.persistentDataPath, "body_tracking_data.json");
        File.WriteAllText(filePath, json);

        Debug.Log($"Movement data saved to {filePath}");
    }

    private string NormalizeBoneName(string boneName)
    {
        return boneName.Replace("_", ""); // Adjust normalization logic if needed
    }

    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> frames;
    }
}
