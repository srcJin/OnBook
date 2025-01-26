using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FullBodyTrackingPlaybackWithAudio : MonoBehaviour
{
    public GameObject avatar; // Reference to the avatar
    public string jsonFileName = "body_tracking_data.json"; // Name of the JSON file to load
    public string audioFileName = "audio_data.wav"; // Name of the audio file to load

    private List<BodyFrameData> playbackData = new List<BodyFrameData>();
    private bool isPlaying = false;
    private bool isPaused = false; // To track pause state
    private float playbackStartTime;
    private float pauseTimeOffset; // To maintain elapsed time during pause
    private int currentFrameIndex = 0;

    private AudioSource audioSource; // AudioSource to play the audio file

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
        Debug.Log("Initializing FullBodyTrackingPlaybackWithAudio...");

        LoadDataFromJson();

        // Cache all the bones of the avatar in order
        avatarBones = avatar.GetComponentsInChildren<Transform>();
        Debug.Log($"Avatar initialization complete. Found {avatarBones.Length} bones.");

        // Initialize AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        LoadAudio();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isPlaying)
            {
                StartPlayback();
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (isPlaying)
            {
                TogglePause();
            }
        }

        if (isPlaying && !isPaused && currentFrameIndex < playbackData.Count)
        {
            float elapsedTime = (Time.time - playbackStartTime) + pauseTimeOffset;

            while (currentFrameIndex < playbackData.Count &&
                   elapsedTime >= playbackData[currentFrameIndex].timestamp)
            {
                ApplyFrameData(playbackData[currentFrameIndex]);
                currentFrameIndex++;
            }

            if (currentFrameIndex >= playbackData.Count)
            {
                EndPlayback();
            }
        }
    }

    void StartPlayback()
    {
        Debug.Log("Playback started...");
        playbackStartTime = Time.time; // Set playback start time
        pauseTimeOffset = 0f; // Reset pause offset
        currentFrameIndex = 0; // Reset to the first frame
        isPlaying = true;
        isPaused = false;

        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource clip is null. Ensure the audio file is loaded properly.");
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // Pause playback
            pauseTimeOffset += Time.time - playbackStartTime; // Calculate elapsed time before pausing
            audioSource.Pause();
            Debug.Log("Playback paused.");
        }
        else
        {
            // Resume playback
            playbackStartTime = Time.time; // Reset playback start time
            audioSource.Play();
            Debug.Log("Playback resumed.");
        }
    }

    void EndPlayback()
    {
        Debug.Log("Playback finished. All frames have been applied.");
        isPlaying = false;
        isPaused = false;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
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

    void LoadAudio()
    {
        string audioPath = Path.Combine(Application.persistentDataPath, audioFileName);

        if (!File.Exists(audioPath))
        {
            Debug.LogError($"Audio file not found at {audioPath}");
            return;
        }

        // Load the audio clip from the file
        WWW audioLoader = new WWW($"file://{audioPath}");
        AudioClip audioClip = audioLoader.GetAudioClip(false, true);

        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            Debug.Log($"Loaded audio file from {audioPath}");
        }
        else
        {
            Debug.LogError($"Failed to load audio from {audioPath}");
        }
    }

    void ApplyFrameData(BodyFrameData frameData)
    {
        if (avatarBones.Length == 0)
        {
            Debug.LogError("No avatar bones found. Ensure the avatar is correctly configured.");
            return;
        }

        foreach (var humanoidBoneData in frameData.humanoidBones)
        {
            string cleanBoneName = humanoidBoneData.boneName.Replace("_", "");

            foreach (HumanBodyBones humanBone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                string humanBoneName = humanBone.ToString();
                if (cleanBoneName.Equals(humanBoneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Transform boneTransform = avatar.GetComponent<Animator>().GetBoneTransform(humanBone);
                    if (boneTransform != null)
                    {
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
        }
    }
}
