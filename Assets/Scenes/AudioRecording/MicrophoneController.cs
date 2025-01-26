using System.IO;
using UnityEngine;

/// <summary>
/// Handles microphone audio recording and saving the recorded audio to a WAV file.
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    private AudioClip recordedClip; // Stores the final recorded audio
    private string _microphone; // Current microphone device in use
    private AudioClip currentlyRecordingClip; // Currently recording audio
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

    /// <summary>
    /// Starts audio recording.
    /// </summary>
    public void StartRecording()
    {
        if (_microphone == null)
        {
            Debug.LogError("No microphone detected. Cannot start recording.");
            return;
        }

        Debug.Log("Microphone recording started...");
        currentlyRecordingClip = Microphone.Start(_microphone, true, 300, 44100);
        recordingStartPosition = Microphone.GetPosition(_microphone);
    }

    /// <summary>
    /// Stops audio recording and saves the recorded audio to a WAV file.
    /// </summary>
    public void StopRecording()
    {
        if (Microphone.IsRecording(_microphone))
        {
            int recordingEndPosition = Microphone.GetPosition(_microphone);
            int totalSamples = recordingEndPosition - recordingStartPosition;
            if (totalSamples < 0) totalSamples += currentlyRecordingClip.samples;

            recordedClip = AudioClip.Create("RecordedClip", totalSamples, currentlyRecordingClip.channels, currentlyRecordingClip.frequency, false);
            float[] samples = new float[totalSamples];
            currentlyRecordingClip.GetData(samples, recordingStartPosition);
            recordedClip.SetData(samples, 0);

            Microphone.End(_microphone);
            SaveRecording(recordedClip);
            Debug.Log("Microphone recording stopped and saved.");
        }
        else
        {
            Debug.LogWarning("No active microphone recording to stop.");
        }
    }

    /// <summary>
    /// Saves the recorded audio to a WAV file.
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
    /// Writes the WAV file header.
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
