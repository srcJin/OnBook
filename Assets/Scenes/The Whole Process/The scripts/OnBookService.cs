using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the recording of both audio and motion data by interfacing with MicrophoneController and MovementRecorder.
/// </summary>
public class OnBookService : MonoBehaviour
{
    MrHonbookClient client;
    private MicrophoneController microphoneController; // Reference to the MicrophoneController component
    private MovementRecorder movementRecorder; // Reference to the MovementRecorder component

    private bool isRecording = false;
    private bool isDataUploaded = false; // Flag to track upload completion

    void Start()
    {
        client = GetComponent<MrHonbookClient>();

        // Automatically assign components if they are on the same GameObject
        microphoneController = GetComponent<MicrophoneController>();
        movementRecorder = GetComponent<MovementRecorder>();

        // Validate that the required components are assigned
        if (microphoneController == null)
        {
            Debug.LogError("MicrophoneController component is missing on this GameObject.");
        }

        if (movementRecorder == null)
        {
            Debug.LogError("MovementRecorder component is missing on this GameObject.");
        }

        if (client == null)
        {
            Debug.LogError("MrHonbookClient component is missing on this GameObject.");
        }
    }

    void Update()
    {
        // Press P to start playback
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isDataUploaded)
            {
                Debug.Log("Starting download and playback...");
                StartCoroutine(DownloadAndPlay());
            }
            else
            {
                Debug.LogWarning("Cannot start playback. Data has not been uploaded yet.");
            }
        }
    }

    /// <summary>
    /// Toggles recording through external interaction, such as poking a button.
    /// </summary>
    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
            StartCoroutine(UploadData());
        }
        else
        {
            StartRecording();
        }
    }

    /// <summary>
    /// Starts recording both audio and motion data.
    /// </summary>
    private void StartRecording()
    {
        Debug.Log("Starting combined recording...");

        if (microphoneController != null)
        {
            microphoneController.StartRecording();
        }

        if (movementRecorder != null)
        {
            movementRecorder.StartRecording();
        }

        isRecording = true;
        isDataUploaded = false; // Reset the upload flag when a new recording starts

        Debug.Log("Combined recording started.");
    }

    /// <summary>
    /// Stops recording both audio and motion data.
    /// </summary>
    private void StopRecording()
    {
        Debug.Log("Stopping combined recording...");

        if (microphoneController != null)
        {
            microphoneController.StopRecording();
        }

        if (movementRecorder != null)
        {
            movementRecorder.StopRecording();
        }

        isRecording = false;

        Debug.Log("Combined recording stopped and saved.");
    }

    /// <summary>
    /// Handles the upload of recorded data and sets the upload completion flag.
    /// </summary>
    private IEnumerator UploadData()
    {
        Debug.Log("Uploading data to the server...");
        yield return StartCoroutine(client.RecordMocapAndAudio());
        isDataUploaded = true; // Set the flag once the upload is complete
        Debug.Log("Data successfully uploaded to the server.");
    }

    /// <summary>
    /// Handles downloading and playing data from the server.
    /// </summary>
    private IEnumerator DownloadAndPlay()
    {
        Debug.Log("Downloading data from the server...");
        yield return StartCoroutine(client.StartPlayback());
        Debug.Log("Playback started successfully.");
    }
}
