using UnityEngine;

/// <summary>
/// Controls the recording of both audio and motion data by interfacing with MicrophoneController and MovementRecorder.
/// </summary>
public class OnBookService : MonoBehaviour
{
    MrHonbookClient client;
    private MicrophoneController microphoneController; // Reference to the MicrophoneController component
    private MovementRecorder movementRecorder; // Reference to the MovementRecorder component

    private bool isRecording = false;

    void Start()
    {
        client = GetComponent<MrHonbookClient>();
        StartCoroutine(client.GetSceneConfig());
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
    }

    void Update()
    {
        // Press Space to toggle recording
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
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
}
