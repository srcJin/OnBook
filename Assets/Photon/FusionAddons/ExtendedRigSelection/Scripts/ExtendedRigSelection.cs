using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion.XR.Shared.Desktop;
using Fusion.Addons.ConnectionManagerAddon;
using Fusion.XR.Shared.Rig;

namespace Fusion.Addons.ExtendedRigSelectionAddon
{
    /***
     * 
     *  ExtendedRigSelection allows to specify and select the local rig that is enabled at runtime.
     *  When the rig selection is done, the connexion handler is enabled : if it contains a `ConnectionManager`, and the selected rig as a custom rig, the `ConnectionManager.userPrefab` will be replaced by the specific NetworkRig.
     *  For each rig, it is possible to set a list of game objects that must enable when the rig is selected (specificGameObjects)
     *  There are several options regarding the rig selection : 
     *      - the user selects the rig at startup using UI buttons,
     *      - the rig is automatically selected using the user's preference file,
     *      - the rig is forced to a specific value between the `RigKindDescription`,
     *  Editor buttons let quickly define the selection mode and associated parameters.
     * 
     ***/

    [DefaultExecutionOrder(ExtendedRigSelection.EXECUTION_ORDER)]
    public class ExtendedRigSelection : MonoBehaviour, IRigSelection
    {
        public const int EXECUTION_ORDER = -10_000;


        [System.Serializable]
        public struct RigKindDescription
        {
            [System.Serializable]
            public struct PrefabToSpawn {
                public GameObject prefab;
                public Transform spawnTransform;
                [DrawIf(nameof(spawnTransform), false, Hide = true)]
                public Vector3 spawnPosition;
                [DrawIf(nameof(spawnTransform), false, Hide = true)]
                public Vector3 spawnRotation;
                [DrawIf(nameof(spawnTransform), Hide = true)]
                public bool destroySpawnTransform;
                public bool isConnexionHandler;
            }

            // Used for both prefs and settings value
            public string name;
            [Header("Specific content")]
            public List<GameObject> specificGameObjects;
            public List<PrefabToSpawn> prefabsToSpawn;
            [Header("Network")]
            // Only relevant if a connexion manager is present on the connexion handler
            public bool useSpecificNetworkRig;
            [DrawIf(nameof(useSpecificNetworkRig), Hide = true)]
            public NetworkObject specificNetworkRig;
            [Tooltip("The object handling the connexion, specific for this rig (will overide ExtendedRigSelection.connexionHandler):\n- it will be disabled until the rig is selected\n - if it contains a ConnectionManager, and the selected rig as a custom rig, the ConnectionManager.userPrefab will be replaced by the specificNetworkRig")]
            public GameObject connexionHandler;
            [HideInInspector]
            public List<GameObject> spawnedObjects;

            public HardwareRig HardwareRig {
                get {
                    if (specificGameObjects == null) return null;
                    foreach (var specificGameObject in specificGameObjects) {
                        if (specificGameObject && specificGameObject.TryGetComponent<HardwareRig>(out var rig))
                        {
                            return rig;
                        }
                    }
                    if(spawnedObjects != null)
                    {
                        foreach (var spawnedObject in spawnedObjects)
                        {
                            if (spawnedObject && spawnedObject.TryGetComponent<HardwareRig>(out var rig))
                            {
                                return rig;
                            }
                        }
                    }
                    return null;
                }
            }
            [Header("Platform")]
            public bool automaticSelectOnTargetPlatform;
            [DrawIf(nameof(automaticSelectOnTargetPlatform), Hide = true)]
            public RuntimePlatform targetPlatform;
        }


        const string VR_KIND_NAME = "VR";
        const string DESKTOP_KIND_NAME = "Desktop";
        const string SETTING_RIGMODE = "RigMode";

        public List<RigKindDescription> rigKindDescriptions = new List<RigKindDescription> {
        new RigKindDescription { name = VR_KIND_NAME, specificGameObjects = new List<GameObject>() },
        new RigKindDescription { name = DESKTOP_KIND_NAME, specificGameObjects = new List<GameObject>() }
    };

        public enum SelectionMode
        {
            SelectedByUI,
            SelectedByUserPref,
            SelectedByForcedValue
        }
        [Tooltip("The object handling the connexion:\n- it will be disabled until the rig is selected\n - if it contains a ConnectionManager, and the selected rig as a custom rig, the ConnectionManager.userPrefab will be replaced by the specificNetworkRig")]
        public GameObject connexionHandler;

        [Header("Selection mode")]
        [Tooltip("How rig is selected: \n- reading user preference \"RigMode\" \n- with UI (in editor only, replaced by pref in builds) \n- forced value (read in forcedKindName field)")]
        public SelectionMode selectionMode = SelectionMode.SelectedByUI;

        [DrawIf(nameof(selectionMode), (long)SelectionMode.SelectedByForcedValue, mode: DrawIfMode.Hide)]
        [Tooltip("Only when the selection mode is SelectedByForcedValue, specify the rig kind name to use")]
        public string forcedKindName = "";

        [Header("Others options")]
        [Tooltip("If true, the VR rig will be always used on Android, no matter the settings (legacy, use automaticSelectOnTargetPlatform now)")]
        public bool forceVROnAndroid = true;

        public UnityEvent OnSelectRig => onSelectRig;

        bool isRigSelected = false;
        public RigKindDescription selectedRig;

        Camera rigSelectionCamera;
        [SerializeField] UnityEvent onSelectRig;

        public bool IsRigSelected => isRigSelected;

        public bool IsVRRigSelected => isRigSelected && selectedRig.name == VR_KIND_NAME;

        private void Awake()
        {
            rigSelectionCamera = GetComponentInChildren<Camera>();
            if (connexionHandler)
            {
                connexionHandler.gameObject.SetActive(false);
            }
            else
            {
                bool noConnexionHandler = true;
                foreach(var rigDescription in rigKindDescriptions)
                {
                    // look for special setup with custom connexion handlers: no warning here as it might be normal to not define a global connexion handler in such cases
                    if (rigDescription.connexionHandler != null)
                    {
                        noConnexionHandler = false;
                        break;
                    }
                    if(rigDescription.prefabsToSpawn != null)
                    {
                        foreach(var prefabInfo in rigDescription.prefabsToSpawn)
                        {
                            if (prefabInfo.isConnexionHandler)
                            {
                                noConnexionHandler = false;
                                break;
                            }
                        }
                    }
                }
                if(noConnexionHandler)
                    Debug.LogError("No connexion handler provided to RigSelection: risk of connection before choosing the appropriate hardware rig !");
            }

            RigKindDescription? automaticSelectedDescription = null;
            GameObject automaticSelectedCustomConnexionHandler = null;
            List<GameObject> automaticalySelectedRigSpecificGameObjects = new List<GameObject>();
            foreach (var rigDescription in rigKindDescriptions)
            {
                if (rigDescription.automaticSelectOnTargetPlatform && Application.platform == rigDescription.targetPlatform && selectionMode != SelectionMode.SelectedByForcedValue)
                {
                    automaticSelectedDescription = rigDescription;
                    automaticalySelectedRigSpecificGameObjects = rigDescription.specificGameObjects;
                    automaticSelectedCustomConnexionHandler = rigDescription.connexionHandler;
                }
                if (selectionMode == SelectionMode.SelectedByForcedValue && rigDescription.name == forcedKindName)
                {
                    automaticSelectedDescription = rigDescription;
                    automaticalySelectedRigSpecificGameObjects = rigDescription.specificGameObjects;
                    automaticSelectedCustomConnexionHandler = rigDescription.connexionHandler;
                    // Overrides any platform specific rig
                    break;
                }
            }


            foreach (var rigDescription in rigKindDescriptions)
            {
                bool disableSpecificGameObjects = true;
                if (automaticSelectedDescription != null && automaticSelectedDescription.GetValueOrDefault().name == rigDescription.name)
                {
                    disableSpecificGameObjects = false;
                }
                if (disableSpecificGameObjects) {
                    foreach (var o in rigDescription.specificGameObjects)
                    {
                        if(o && automaticalySelectedRigSpecificGameObjects.Contains(o) == false) o.SetActive(false);
                    }
                }
                if (rigDescription.connexionHandler && automaticSelectedCustomConnexionHandler != rigDescription.connexionHandler && automaticalySelectedRigSpecificGameObjects.Contains(rigDescription.connexionHandler) == false)
                {
                    rigDescription.connexionHandler.SetActive(false);
                }
            }

            if (automaticSelectedDescription != null)
            {
                EnableRig(automaticSelectedDescription.GetValueOrDefault());
                return;
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            if (forceVROnAndroid)
            {
                EnableRig(VR_KIND_NAME);
                return;
            }
#endif

            // In release build, we replace SelectedByUI by SelectedByUserPref unless overriden
            DisableDebugSelectedByUI();

            if (selectionMode == SelectionMode.SelectedByUserPref)
            {
                var rigDescription = PreferedRigDescription();
                EnableRig(rigDescription);
            }
            else if (selectionMode == SelectionMode.SelectedByForcedValue)
            {
                EnableRig(forcedKindName);
            }
        }

        public RigKindDescription? PreferedRigDescription(bool silentSearch = false)
        {
            var sessionPrefMode = PlayerPrefs.GetString(SETTING_RIGMODE);
            RigKindDescription? rigDescription = null;
            if (sessionPrefMode != "")
            {
                rigDescription = FindRigDescriptionByName(sessionPrefMode, silentSearch);
            }
            return rigDescription;
        }

        RigKindDescription? FindRigDescriptionByName(string name, bool silentSearch = false)
        {
            foreach (var rigDescription in rigKindDescriptions)
            {
                if (rigDescription.name == name)
                {
                    return rigDescription;
                }
            }
            if (silentSearch == false) Debug.LogError($"Unable to find {name} rig");

            return null;
        }

        void EnableRig(string name, bool silentSearch = false)
        {
            var rigDescription = FindRigDescriptionByName(name, silentSearch);
            EnableRig(rigDescription);
        }

        public void EnableRig(RigKindDescription? rigDescription)
        {
            isRigSelected = false;
            if (rigDescription != null)
            {
                SelectRig(rigDescription.GetValueOrDefault());
            }
        }

        void SelectRig(RigKindDescription rigDescription)
        {
            isRigSelected = true;
            selectedRig = rigDescription;
            foreach (var o in rigDescription.specificGameObjects)
            {
                if(o) o.SetActive(true);
            }
            SavePreference(rigDescription);

            var selectedConnexionHandler = connexionHandler;
            if (rigDescription.connexionHandler != null)
            {
                selectedConnexionHandler = rigDescription.connexionHandler;
            }

            // Prefab spawing
            rigDescription.spawnedObjects = new List<GameObject>();
            if (rigDescription.prefabsToSpawn != null)
            {
                foreach (var prefabInfo in rigDescription.prefabsToSpawn)
                {
                    if (prefabInfo.prefab == null) continue;
                    var pos = prefabInfo.spawnTransform ? prefabInfo.spawnTransform.position : prefabInfo.spawnPosition;
                    var rot = prefabInfo.spawnTransform ? prefabInfo.spawnTransform.rotation : Quaternion.Euler(prefabInfo.spawnRotation);
                    GameObject spawnedObject;
                    if (prefabInfo.spawnTransform && prefabInfo.spawnTransform.parent != null)
                    {
                        spawnedObject = GameObject.Instantiate(prefabInfo.prefab, pos, rot, parent: prefabInfo.spawnTransform.parent);
                    }
                    else
                    {
                        spawnedObject = GameObject.Instantiate(prefabInfo.prefab, pos, rot);
                    }
                    if (prefabInfo.spawnTransform)
                    {
                        spawnedObject.transform.SetSiblingIndex(prefabInfo.spawnTransform.GetSiblingIndex() + 1);
                    }
                    rigDescription.spawnedObjects.Add(spawnedObject);
                    if (prefabInfo.isConnexionHandler)
                    {
                        selectedConnexionHandler = spawnedObject;
                    }
                }
                foreach (var prefabInfo in rigDescription.prefabsToSpawn)
                {
                    if (prefabInfo.spawnTransform != null && prefabInfo.destroySpawnTransform)
                    {
                        GameObject.Destroy(prefabInfo.spawnTransform.gameObject);
                    }
                }
            }

            if (rigDescription.useSpecificNetworkRig && selectedConnexionHandler && selectedConnexionHandler.TryGetComponent(out ConnectionManager connexionManager))
            {
                connexionManager.userPrefab = rigDescription.specificNetworkRig;
            }

            OnRigSelected(selectedConnexionHandler);
        }

        public void SavePreference(RigKindDescription rigDescription)
        {
            SavePreference(rigDescription.name);
        }

        public static void SavePreference(string rigName)
        {
            PlayerPrefs.SetString(SETTING_RIGMODE, rigName);
            PlayerPrefs.Save();
        }

        void OnRigSelected(GameObject selectedConnexionHandler)
        {
            gameObject.SetActive(false);
            if (OnSelectRig != null) OnSelectRig.Invoke();

            if (selectedConnexionHandler && selectedConnexionHandler.gameObject.activeSelf == false)
            {

                selectedConnexionHandler.gameObject.SetActive(true);

                var runner = selectedConnexionHandler.GetComponent<NetworkRunner>();
                if (runner)
                {
                    // As the runner was disabled, the runner may not have auto registered its listeners
                    foreach(var listener in runner.GetComponentsInChildren<INetworkRunnerCallbacks>())
                    {
                        runner.AddCallbacks(listener);
                    }
                }
            }
            if (rigSelectionCamera) rigSelectionCamera.gameObject.SetActive(false);
            isRigSelected = true;
        }

        protected virtual void DisableDebugSelectedByUI()
        {
#if !UNITY_EDITOR
        if (selectionMode == SelectionMode.SelectedByUI) selectionMode = SelectionMode.SelectedByUserPref;
#endif
        }

        protected virtual void OnGUI()
        {
            GUILayout.BeginArea(new Rect(5, 5, Screen.width - 10, Screen.height - 10));
            {
                GUILayout.BeginVertical(GUI.skin.window);
                {
                    foreach (var rigDescription in rigKindDescriptions)
                    {
                        if (GUILayout.Button(rigDescription.name))
                        {
                            EnableRig(rigDescription.name);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }
    }
}
