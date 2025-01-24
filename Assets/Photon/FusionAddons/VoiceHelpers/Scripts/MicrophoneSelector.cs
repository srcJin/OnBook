using Photon.Voice;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Photon.Voice.Fusion;

[RequireComponent(typeof(Recorder))]
// Simple component to select microphone in the editor
public class MicrophoneSelector : VoiceComponent
{
    IDeviceEnumerator photonMicEnum;
    FusionVoiceClient _fusionVoiceClient;
    FusionVoiceClient FusionVoiceClient {
        get
        {
            if (_fusionVoiceClient == null)
            {
                _fusionVoiceClient = FindObjectOfType<FusionVoiceClient>();
            }
            return _fusionVoiceClient;
        }
    }

    Recorder _recorder;
    Recorder Recorder
    {
        get
        {
            if(_recorder == null)
            {
                _recorder = GetComponent<Recorder>();
            }
            return _recorder;
        }
    }

    bool wasPreferenceSet = false;

    public bool IsRecorderConnected => Recorder && FusionVoiceClient && FusionVoiceClient.Client != null && FusionVoiceClient.ClientState == Photon.Realtime.ClientState.Joined;

    public bool isMicrophoneFound = false;
    public List<string> microphoneNames = new List<string>();
    public int selectedMicrophoneIndex = -1;

    private void Update()
    {
        if (wasPreferenceSet == false && IsRecorderConnected)
        {
            wasPreferenceSet = true;
            SelectMicrophoneWithName(GetStoredSelectedMicrophoneName());
        }

    }

    const string MICROPHONE_SELECTOR_PREF = "MICROPHONE_SELECTOR_PREF";
    public string GetStoredSelectedMicrophoneName()
    {
        return PlayerPrefs.GetString(MICROPHONE_SELECTOR_PREF);
    }

    public void SetStoredSelectedMicrophoneName(string name)
    {
        if (name == GetStoredSelectedMicrophoneName())
        {
            return;
        }
        PlayerPrefs.SetString(MICROPHONE_SELECTOR_PREF, name);
        PlayerPrefs.Save();
    }

    public void SelectMicrophoneAtIndex(int index)
    {
#if !UNITY_WEBGL
        if (Recorder == null) return;
        var devices = MicrophonesDeviceInfos();
        if (index < devices.Count)
        {
            ChangeRecorderMicrophone(devices[index]);
        }
        RefreshMicrophoneList();
#endif 
    }

    public void SelectMicrophoneWithName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
#if !UNITY_WEBGL
        if (Recorder == null) return;
        var devices = MicrophonesDeviceInfos();
        foreach(var d in devices)
        {
            if (d.Name == name)
            {
                ChangeRecorderMicrophone(d);
            }
        }
        RefreshMicrophoneList();
#endif 
    }

    void ChangeRecorderMicrophone(DeviceInfo d)
    {
        if (IsRecorderConnected)
        {
            Recorder.MicrophoneDevice = d;
        }
        SetStoredSelectedMicrophoneName(d.Name);
    }

    public List<DeviceInfo> MicrophonesDeviceInfos()
    {
        List<DeviceInfo> deviceInfos = new List<DeviceInfo>();
        if (Recorder == null) return deviceInfos;
        microphoneNames.Clear();
#if !UNITY_WEBGL
        if (Recorder.MicrophoneType == Recorder.MicType.Unity)
        {
            var micNames = new List<string>(Microphone.devices);
            foreach(var micName in micNames)
            {
                deviceInfos.Add(new DeviceInfo(micName));
                microphoneNames.Add(micName);
            }
        }
        else
        {
            if (photonMicEnum == null)
            {
                photonMicEnum = Platform.CreateAudioInEnumerator(this.Logger);
            }
            photonMicEnum.Refresh();
            if (photonMicEnum.IsSupported)
            {
                foreach (var device in photonMicEnum)
                {
                    deviceInfos.Add(device);
                    microphoneNames.Add(device.Name);
                }
            }                
        }
#endif
        return deviceInfos;
    }

    [ContextMenu("RefreshMicrophoneList")]
    public void RefreshMicrophoneList()
    {
        selectedMicrophoneIndex = -1;
        isMicrophoneFound = false;
        if(Recorder == null) return;

        var deviceInfos = MicrophonesDeviceInfos();
        if (deviceInfos.Count > 0)
        {
#if !UNITY_WEBGL
            var selectedName = IsRecorderConnected ? Recorder.MicrophoneDevice.Name : GetStoredSelectedMicrophoneName();
            // Search for the microphone index
            if (Recorder.MicrophoneType == Recorder.MicType.Unity)
            {
                // Unity microphone
                if ((IsRecorderConnected || string.IsNullOrEmpty(GetStoredSelectedMicrophoneName())) && Recorder.MicrophoneDevice.IsDefault)
                {
                    selectedMicrophoneIndex = 0;
                }
                else
                {
                    selectedMicrophoneIndex = deviceInfos.FindIndex(device => device.Name == selectedName);
                }
            }
            else
            {   // Photon microphone
                int i = 0;
                foreach (var device in deviceInfos)
                {
                    if (device.Name == selectedName)
                    {
                        selectedMicrophoneIndex = i;
                    }
                    i++;
                }
            }
#endif
        }
        isMicrophoneFound = selectedMicrophoneIndex != -1;
    }
}
