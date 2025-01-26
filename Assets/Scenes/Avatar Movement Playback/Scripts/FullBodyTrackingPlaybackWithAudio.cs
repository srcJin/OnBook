using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class FullBodyTrackingPlaybackWithAudio : MonoBehaviour
{
    public GameObject avatar; // Reference to the avatar
    public string jsonFileName = "body_tracking_data.json"; // Name of the JSON file to load
    public string audioFileName = "audio_data.wav"; // Name of the audio file to load

    public GameObject play;  // Reference to the play button
    public GameObject pause; // Reference to the pause button

    private List<BodyFrameData> playbackData = new List<BodyFrameData>();
    private bool isPlaying = false;
    private bool isPaused = false; // To track pause state
    private float playbackStartTime;
    private float pauseTimeOffset; // To maintain elapsed time during pause
    private int currentFrameIndex = 0;

    private AudioSource audioSource; // AudioSource to play the audio file
    private Dictionary<string, Transform> boneTransforms; // Efficient mapping of bone names to transforms

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

    void Start()
    {
        Debug.Log("Initializing FullBodyTrackingPlaybackWithAudio...");

        LoadDataFromJson();
        CacheBoneTransforms();
        InitializeAudioSource();

        play.SetActive(true);
        pause.SetActive(false);
    }

    /// <summary>
    /// Public function to toggle playback, linked to UI button.
    /// </summary>
    public void TogglePlayback()
    {
        if (!isPlaying || isPaused)
        {
            StartPlayback();
        }
        else
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Starts playback of motion and audio.
    /// </summary>
    public void StartPlayback()
    {
        Debug.Log("Playback started...");
        if (!isPlaying)
        {
            playbackStartTime = Time.time;
            pauseTimeOffset = 0f;
            currentFrameIndex = 0; // Reset to the first frame
        }
        else if (isPaused)
        {
            playbackStartTime = Time.time; // Adjust start time for resuming
        }

        isPlaying = true;
        isPaused = false;

        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource clip is null or already playing. Ensure the audio file is loaded properly.");
        }

        play.SetActive(false);
        pause.SetActive(true);
    }

    /// <summary>
    /// Toggles the pause state of playback.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            playbackStartTime = Time.time; // Adjust start time for resuming
            audioSource.Play();
            Debug.Log("Playback resumed.");
        }
        else
        {
            pauseTimeOffset += Time.time - playbackStartTime; // Calculate elapsed time before pausing
            audioSource.Pause();
            Debug.Log("Playback paused.");
        }

        isPaused = !isPaused;

        // Update UI based on pause state
        play.SetActive(isPaused);
        pause.SetActive(!isPaused);
    }

    void Update()
    {
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

    void EndPlayback()
    {
        Debug.Log("Playback finished. All frames have been applied.");
        isPlaying = false;
        isPaused = false;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Reset play and pause buttons
        play.SetActive(true);
        pause.SetActive(false);
    }

    /// <summary>
    /// Loads playback data from a JSON file.
    /// </summary>
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

    /// <summary>
    /// Caches the avatar's bone transforms for efficient lookup.
    /// </summary>
    void CacheBoneTransforms()
    {
        boneTransforms = new Dictionary<string, Transform>();
        Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>();

        foreach (var bone in allTransforms)
        {
            boneTransforms[bone.name] = bone;
        }

        Debug.Log($"Cached {boneTransforms.Count} bone transforms.");
    }

    /// <summary>
    /// Loads the audio file for playback.
    /// </summary>
    void InitializeAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        string audioPath = Path.Combine(Application.persistentDataPath, audioFileName);

        if (!File.Exists(audioPath))
        {
            Debug.LogError($"Audio file not found at {audioPath}");
            return;
        }

        var audioLoader = new WWW($"file://{audioPath}");
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

    /// <summary>
    /// Applies the frame data to the avatar.
    /// </summary>
    void ApplyFrameData(BodyFrameData frameData)
    {
        foreach (var humanoidBoneData in frameData.humanoidBones)
        {
            if (boneTransforms.TryGetValue(humanoidBoneData.boneName, out Transform boneTransform))
            {
                boneTransform.localPosition = humanoidBoneData.position;
                boneTransform.localRotation = humanoidBoneData.rotation;
            }
            else
            {
                Debug.LogWarning($"Bone '{humanoidBoneData.boneName}' not found in the cached transforms.");
            }
        }
    }
}
