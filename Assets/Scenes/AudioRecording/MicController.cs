using System.IO;
using UnityEngine;

/// <summary>
/// 动态录音脚本，开始和结束录音动态计算长度，无需提前定义
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    private AudioClip recordedClip; // 保存最终录音的音频
    private string _microphone; // 当前使用的麦克风设备
    private AudioClip currentlyRecordingClip; // 正在录制的音频
    private float[] audioSamples = new float[256]; // 用于计算声音强度的缓冲
    private float elapsedSeconds = 0f; // 每秒记录音量
    private int recordingStartPosition; // 录音开始的位置

    private void Awake()
    {
        // 设置使用的麦克风设备
        if (Microphone.devices.Length > 0)
        {
            _microphone = Microphone.devices[0];
            Debug.Log($"检测到麦克风: {_microphone}");
        }
        else
        {
            Debug.LogError("未检测到麦克风设备。");
            return;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) // 开始录音
        {
            StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.S)) // 停止录音
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
    /// 开始录音
    /// </summary>
    private void StartRecording()
    {
        Debug.Log("开始录音...");
        if (Microphone.IsRecording(_microphone))
        {
            StopRecording();
        }

        // 开始录音，无限时长
        currentlyRecordingClip = Microphone.Start(_microphone, true, 300, 44100);
        recordingStartPosition = Microphone.GetPosition(_microphone);
        Debug.Log($"已开始录音，设备: {_microphone}");
    }

    /// <summary>
    /// 停止录音
    /// </summary>
    private void StopRecording()
    {
        if (Microphone.IsRecording(_microphone))
        {
            int recordingEndPosition = Microphone.GetPosition(_microphone);
            int totalSamples = recordingEndPosition - recordingStartPosition;
            if (totalSamples < 0) totalSamples += currentlyRecordingClip.samples; // 处理循环缓冲区
            recordedClip = AudioClip.Create("RecordedClip", totalSamples, currentlyRecordingClip.channels, currentlyRecordingClip.frequency, false);

            // 从录音缓冲中提取实际音频数据
            float[] samples = new float[totalSamples];
            currentlyRecordingClip.GetData(samples, recordingStartPosition);
            recordedClip.SetData(samples, 0);

            Microphone.End(_microphone);
            SaveRecording(recordedClip);
            Debug.Log("录音已停止并保存。");
        }
        else
        {
            Debug.LogWarning("当前没有正在进行的录音。");
        }
    }

    /// <summary>
    /// 输出当前声音强度
    /// </summary>
    private void LogSoundLevel()
    {
        int micPosition = Microphone.GetPosition(_microphone);
        if (micPosition >= audioSamples.Length)
        {
            currentlyRecordingClip.GetData(audioSamples, micPosition - audioSamples.Length);
            float rmsValue = Mathf.Sqrt(CalculateRMS(audioSamples));
            Debug.Log($"当前声音强度 (RMS): {rmsValue:F3}");
        }
    }

    /// <summary>
    /// 计算音频样本的 RMS 值
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
    /// 保存录制的音频到 WAV 文件
    /// </summary>
    private void SaveRecording(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("没有录制的音频可保存。");
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

        Debug.Log($"录音已保存到 {filePath}");
    }

    /// <summary>
    /// 写入 WAV 文件头
    /// </summary>
    private void WriteWavHeader(BinaryWriter writer, int channels, int sampleRate, int totalSamples)
    {
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + totalSamples * 2);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM 格式
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2)); // 数据块对齐
        writer.Write((short)16); // 每个采样的位数
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(totalSamples * 2);
    }
}