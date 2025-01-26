using System.IO;
using UnityEngine;

/// <summary>
/// Dynamic recording script that dynamically calculates the length of the recording without the need for predefined duration.
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    private AudioClip recordedClip; // Stores the final recorded audio
    private string _microphone; // Current microphone device in use
    private AudioClip currentlyRecordingClip; // Currently recording audio
    private float[] audioSamples = new float[256]; // Buffer for calculating sound intensity
    private float elapsedSeconds = 0f; // Tracks elapsed time to log sound level every second
    private int recordingStartPosition; // Position where the recording started

    private void Awake()
    {
        // Set the microphone device to use
        if (Microphone.devices.Length > 0)
        {
            _microphone = Microphone.devices[0];
            Debug.Log($"Microphone detected: {_microphone}");
        }
        else
        {
            Debug.LogError("No microphone devices detected.");
            return;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) // Start recording
        {
            StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.S)) // Stop recording
        {
            StopRecording();
        }

        if (currentlyRecordingClip != null && Microphone.IsRecording(_microphone))
        {
            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds >= 1f)
            {
                LogSoundLevel();
                elapsedSeconds = 0f;
            }
        }
    }

    /// <summary>
    /// Start recording
    /// </summary>
    private void StartRecording()
    {
        Debug.Log("Recording started...");
        if (Microphone.IsRecording(_microphone))
        {
            StopRecording();
        }

        // Start recording with an indefinite duration
        currentlyRecordingClip = Microphone.Start(_microphone, true, 300, 44100);
        recordingStartPosition = Microphone.GetPosition(_microphone);
        Debug.Log($"Recording started, device: {_microphone}");
    }

    /// <summary>
    /// Stop recording
    /// </summary>
    private void StopRecording()
    {
        if (Microphone.IsRecording(_microphone))
        {
            int recordingEndPosition = Microphone.GetPosition(_microphone);
            int totalSamples = recordingEndPosition - recordingStartPosition;
            if (totalSamples < 0) totalSamples += currentlyRecordingClip.samples; // Handle circular buffer
            recordedClip = AudioClip.Create("RecordedClip", totalSamples, currentlyRecordingClip.channels, currentlyRecordingClip.frequency, false);

            // Extract the actual audio data from the recording buffer
            float[] samples = new float[totalSamples];
            currentlyRecordingClip.GetData(samples, recordingStartPosition);
            recordedClip.SetData(samples, 0);

            Microphone.End(_microphone);
            SaveRecording(recordedClip);
            Debug.Log("Recording stopped and saved.");
        }
        else
        {
            Debug.LogWarning("No ongoing recording to stop.");
        }
    }

    /// <summary>
    /// Log the current sound intensity
    /// </summary>
    private void LogSoundLevel()
    {
        int micPosition = Microphone.GetPosition(_microphone);
        if (micPosition >= audioSamples.Length)
        {
            currentlyRecordingClip.GetData(audioSamples, micPosition - audioSamples.Length);
            float rmsValue = Mathf.Sqrt(CalculateRMS(audioSamples));
            Debug.Log($"Current sound intensity (RMS): {rmsValue:F3}");
        }
    }

    /// <summary>
    /// Calculate the RMS value of the audio samples
    /// </summary>
    private float CalculateRMS(float[] samples)
    {
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += sample * sample;
        }
        return sum / samples.Length;
    }

    /// <summary>
    /// Save the recorded audio to a WAV file
    /// </summary>
    private void SaveRecording(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("No recorded audio to save.");
            return;
        }

        string filePath = Path.Combine(Application.persistentDataPath, "audio_data.wav");
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fileStream))
        {
            WriteWavHeader(writer, clip.channels, clip.frequency, clip.samples);
            foreach (float sample in samples)
            {
                writer.Write((short)(sample * 32767));
            }
        }

        Debug.Log($"Recording saved to {filePath}");
    }

    /// <summary>
    /// Write the WAV file header
    /// </summary>
    private void WriteWavHeader(BinaryWriter writer, int channels, int sampleRate, int totalSamples)
    {
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + totalSamples * 2);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM format
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2)); // Block align
        writer.Write((short)16); // Bits per sample
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(totalSamples * 2);
    }
}
