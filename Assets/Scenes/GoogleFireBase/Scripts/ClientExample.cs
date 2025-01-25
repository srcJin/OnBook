using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MrHonbookClient : MonoBehaviour
{
    private string currentSceneId = "scene001";
    private string ThisClientId = "testClient456"; //THIS SHOULD BE HARD CODED BASED ON CLIENTS
    private string webserverURL = "https://mrhonbook-132d3a53c108.herokuapp.com";
    private string firebaseBucket = "mrhonbook.firebasestorage.app";

    ///////////////////////////////////////////////////////////////////////////
    // 1) EXAMPLE: On connection, READ the current scene configuration
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator GetSceneConfig()
    {
        string url = webserverURL + "/api/currentscene";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error getting scene config: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;

            SceneConfigWrapper data = null;
            try
            {
                data = JsonConvert.DeserializeObject<SceneConfigWrapper>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Deserialization error: " + ex);
                yield break;
            }

            if (data == null || data.samples == null || data.samples.Count == 0)
            {
                Debug.LogWarning("Scene config is empty or doesn't match the expected format.");
                yield break;
            }

            // Download and handle associated files for each sample
            foreach (KeyValuePair<string, SampleInfo> kvp in data.samples)
            {
                
                string key = kvp.Key; 
                Debug.Log(key);    
                if (!string.IsNullOrEmpty(key))
                {
                    yield return StartCoroutine(HandleMocapAndAudio(key + "Mocap", key + "Audio"));
                }
            }
        }
    }

    [Serializable]
    public class SceneConfigWrapper {
        public Dictionary<string, SampleInfo> samples;
    }

    [Serializable]
    public class SampleInfo {
        public long endedAt;
        public string recordedAudio;
        public string recordedMocap;
        public string clientId;

    }

    ///////////////////////////////////////////////////////////////////////////
    // 1.5) EXAMPLE: On connection, READ the current sceneId and store it
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator GetCurrentSceneId()
    {
        string url = webserverURL + "/api/currentsceneid";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error getting current scene id: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("Raw JSON: " + json);

            SceneIDInfo data = null;
            try
            {
                data = JsonConvert.DeserializeObject<SceneIDInfo>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Deserialization error: " + ex);
                yield break;
            }

            if (data == null || string.IsNullOrEmpty(data.currentSceneId))
            {
                Debug.LogWarning("Scene ID is empty or doesn't match the expected format.");
                yield break;
            }

            // Store the current scene ID locally
            currentSceneId = data.currentSceneId;
            Debug.Log("Updated currentSceneId to: " + currentSceneId);
        }
    }

    [Serializable]
    public class SceneIDInfo
    {
        [JsonProperty("currentSceneId")]
        public string currentSceneId;
    }


    ///////////////////////////////////////////////////////////////////////////
    // 2) EXAMPLE: On recording end, UPDATE audio and mocap data for this client
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator RecordMocapAndAudio()
    {
        //TODO: Handle getting local stored data files
        string mocapLocalPath = Path.Combine(Application.dataPath, "Scripts/MichaelExampleScripts/mrhonbook-default-rtdb-export.json");
        string audioLocalPath = Path.Combine(Application.dataPath, "Scripts/MichaelExampleScripts/file_example_MP3_5MG.mp3");

        yield return StartCoroutine(UploadFileDirect(mocapLocalPath, ThisClientId + "Mocap", "application/json"));
        string mocapDownloadURL = lastUploadedUrl;

        yield return StartCoroutine(UploadFileDirect(audioLocalPath, ThisClientId + "Audio", "audio/mpeg"));
        string audioDownloadURL = lastUploadedUrl;

        if (string.IsNullOrEmpty(mocapDownloadURL) || string.IsNullOrEmpty(audioDownloadURL))
        {
            Debug.LogError("Missing URLs for uploaded files");
            yield break;
        }

        // 3) Update server with references
        string endpoint = webserverURL + "/api/endrecording";
        EndRecordingPayload payload = new EndRecordingPayload
        {
            clientId  = ThisClientId,
            sceneId   = currentSceneId,
            mocapData = mocapDownloadURL,
            audioData = audioDownloadURL
        };

        // Serialize to JSON with Newtonsoft
        string jsonBody = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error updating end recording: " + request.error);
            }
            else
            {
                Debug.Log("End recording updated: " + request.downloadHandler.text);
            }
        }
    }

    [Serializable]
    public class EndRecordingPayload
    {
        public string clientId;
        public string sceneId;
        public string mocapData;
        public string audioData;
    }

    ///////////////////////////////////////////////////////////////////////////
    // 3) EXAMPLE: On annotation end, UPDATE annotation for this client
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator RecordAnnotation(object data)
    {
        //TODO: Use passed data
        string endpoint = webserverURL + "/api/annotation";
        AnnotationPayload payload = new AnnotationPayload
        {
            annotationData = "some new annotation data " + DateTime.Now.Ticks,
            sceneId        = currentSceneId,
            clientId       = ThisClientId
        };

        // Convert to JSON with Newtonsoft
        string jsonBody = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error recording annotation: " + request.error);
            }
            else
            {
                Debug.Log("Annotation response: " + request.downloadHandler.text);
            }
        }
    }

    [Serializable]
    public class AnnotationPayload
    {
        public string annotationData;
        public string sceneId;
        public string clientId;
    }

    ///////////////////////////////////////////////////////////////////////////
    // 4) EXAMPLE: On library change, UPDATE the current scene THEN get config
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator UpdateCurrentScene(string desiredSceneId)
    {
        string endpoint = $"{webserverURL}/api/scenechanged/{desiredSceneId}";

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error updating current scene: " + request.error);
            }
            else
            {
                Debug.Log("Scene changed response: " + request.downloadHandler.text);
                currentSceneId = desiredSceneId;

                // After updating, fetch new scene config
                yield return StartCoroutine(GetSceneConfig());
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    // 5) EXAMPLE: On playback start, READ existing mocap/audio for other clients
    ///////////////////////////////////////////////////////////////////////////
    public IEnumerator StartPlayback()
    {
        // 1) Prepare the POST request to /api/playback
        string endpoint = webserverURL + "/api/playback";
        PlaybackPayload payload = new PlaybackPayload
        {
            clientId = ThisClientId,
            sceneId  = currentSceneId
        };

        // 2) Serialize the payload with Newtonsoft
        string jsonBody = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 3) Send the request and wait
            yield return request.SendWebRequest();

            // 4) Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error starting playback: " + request.error);
                yield break;
            }

            // 5) Get the server response as text
            string responseText = request.downloadHandler.text;

            // 6) Try to deserialize into a Dictionary<string, PlaybackEntry>
            Dictionary<string, PlaybackEntry> playbackDict = null;

            try
            {
                playbackDict = JsonConvert.DeserializeObject<Dictionary<string, PlaybackEntry>>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing response: " + e);
                yield break;
            }

            // 7) If we got valid data, iterate the dictionary
            if (playbackDict != null)
            {
                foreach (var kvp in playbackDict)
                {
                    // 8) Download & handle audio/mocap for this client
                    yield return StartCoroutine(HandleMocapAndAudio(
                        kvp.Key + "Mocap", 
                        kvp.Key + "Audio"
                    ));
                }
            }
            else
            {
                Debug.LogWarning("Playback data is empty or not in the expected format.");
            }
        }
    }

    // Example data structure for your POST body
    [Serializable]
    public class PlaybackPayload
    {
        public string clientId;
        public string sceneId;
    }

    // Example data structure for your playback entries
    [Serializable]
    public class PlaybackEntry
    {
        public string clientId;
        // any other fields your server returns
    }

    ///////////////////////////////////////////////////////////////////////////
    // SHARED: Download "mocap" JSON and "audio" file, parse or play
    ///////////////////////////////////////////////////////////////////////////
    private IEnumerator HandleMocapAndAudio(string mocapFileName, string audioFileName)
    {
        //TODO: Check if there is a local copy of external data
        // 1) Download mocap as bytes
        yield return StartCoroutine(DownloadFileAsBytes(mocapFileName));
        byte[] mocapData = lastDownloadedBytes;
        if (mocapData != null)
        {
            string mocapText = Encoding.UTF8.GetString(mocapData);
            Debug.Log($"Decoded Mocap JSON ({mocapFileName}):\n{mocapText}");

            //TODO: Store external mocap files
        }

        // 2) Download audio as bytes
        yield return StartCoroutine(DownloadFileAsBytes(audioFileName));
        byte[] audioData = lastDownloadedBytes;
        if (audioData != null)
        {
            string audioText = Encoding.UTF8.GetString(audioData);
            Debug.Log($"Decoded Audio Bytes ({audioFileName}):\n{audioText}");
            
            //TODO: Store external audio files
        }

        //TODO: Play external and local audio and mocap files synchronously

    }

    ///////////////////////////////////////////////////////////////////////////
    // HELPERS: Direct Upload to Firebase Storage (Unauthenticated)
    ///////////////////////////////////////////////////////////////////////////
    private string lastUploadedUrl;

    private IEnumerator UploadFileDirect(string localPath, string fileName, string contentType)
    {
        lastUploadedUrl = null;

        if (!File.Exists(localPath))
        {
            Debug.LogError("File not found: " + localPath);
            yield break;
        }
        byte[] fileData = File.ReadAllBytes(localPath);

        string objectPath = UnityWebRequest.EscapeURL($"uploads/{currentSceneId}/{fileName}");
        string uploadUrl = $"https://firebasestorage.googleapis.com/v0/b/{firebaseBucket}/o?uploadType=media&name={objectPath}";

        UnityWebRequest request = new UnityWebRequest(uploadUrl, "POST");
        request.uploadHandler   = new UploadHandlerRaw(fileData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", contentType);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Upload error: {request.error}\nResponse: {request.downloadHandler.text}");
            yield break;
        }

        // On success, Firebase returns JSON describing the object
        string responseJson = request.downloadHandler.text;
        Debug.Log($"Upload success for {fileName}. Response:\n{responseJson}");

        // Parse with Newtonsoft
        FirebaseUploadResponse uploadInfo = null;
        try
        {
            uploadInfo = JsonConvert.DeserializeObject<FirebaseUploadResponse>(responseJson);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing upload response: " + ex);
        }

        if (uploadInfo != null && !string.IsNullOrEmpty(uploadInfo.name))
        {
            lastUploadedUrl = BuildFirebaseDownloadUrl(uploadInfo.name, uploadInfo.downloadTokens);
        }

        request.Dispose();
    }

    [Serializable]
    public class FirebaseUploadResponse
    {
        public string name;
        public string bucket;
        public string downloadTokens;
        public string mediaLink; // optional
    }

    // Build a direct read URL
    private string BuildFirebaseDownloadUrl(string objectName, string token = null)
    {
        string encoded = UnityWebRequest.EscapeURL(objectName);
        string url = $"https://firebasestorage.googleapis.com/v0/b/{firebaseBucket}/o/{encoded}?alt=media";
        if (!string.IsNullOrEmpty(token))
        {
            url += $"&token={token}";
        }
        return url;
    }

    ///////////////////////////////////////////////////////////////////////////
    // HELPERS: Direct Download from Firebase Storage (Unauthenticated)
    ///////////////////////////////////////////////////////////////////////////
    private byte[] lastDownloadedBytes;

    private IEnumerator DownloadFileAsBytes(string fileName)
    {
        lastDownloadedBytes = null;

        string url = BuildFirebaseDownloadUrl("uploads/" + currentSceneId + "/" + fileName);

        Debug.Log(url);
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            lastDownloadedBytes = request.downloadHandler.data;
            Debug.Log($"Downloaded {fileName}, size: {lastDownloadedBytes.Length} bytes");
        }
        else
        {
            Debug.LogError("Download error: " + request.error);
        }
        request.Dispose();
    }
}

/// <summary>
/// Example annotation payload for the RecordAnnotation call
/// </summary>
[Serializable]
public class AnnotationPayload
{
    public string annotationData;
    public string sceneId;
    public string clientId;
}

/// <summary>
/// Example playback payload for the StartPlayback call
/// </summary>
[Serializable]
public class PlaybackPayload
{
    public string clientId;
    public string sceneId;
}

