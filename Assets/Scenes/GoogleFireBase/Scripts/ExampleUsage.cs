using UnityEngine;
using System;
using System.Collections.Generic;


public class ExampleUsage : MonoBehaviour
{
    MrHonbookClient client;

    void Start()
    {
        client = GetComponent<MrHonbookClient>();
        // StartCoroutine(client.GetSceneConfig());
        // StartCoroutine(client.GetCurrentSceneId());

        // StartCoroutine(client.RecordMocapAndAudio());
        // StartCoroutine(client.StartPlayback());

        // StartCoroutine(client.RecordAnnotation( null ));
        // StartCoroutine(client.UpdateCurrentScene( "scene001"));
    }



    //LIBRARY HARD CODED DATA FOR LATER
    [System.Serializable]
    public class SceneInfo
    {
        public string name;
        public string lastRecorded;
    }

    // Hard-coded dictionary for quick reference
    private Dictionary<string, SceneInfo> sceneData = new Dictionary<string, SceneInfo>
    {
        {
            "scene001",
            new SceneInfo
            {
                name = "Act 1 Scene 1",
                lastRecorded = "February 19st, 2025"
            }
        },
        {
            "scene002",
            new SceneInfo
            {
                name = "Act 2 Scene 4",
                lastRecorded = "January 22th, 2025"
            }
        },
        {
            "scene003",
            new SceneInfo
            {
                name = "Act 4 Scene 5",
                lastRecorded = "January 25th, 2025"
            }
        }
    };
}
