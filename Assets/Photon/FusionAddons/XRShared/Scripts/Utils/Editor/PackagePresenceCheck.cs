#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Fusion.XRShared.Tools
{
    public class PackagePresenceCheck
    {
        string[] packageNames = null;
        UnityEditor.PackageManager.Requests.ListRequest request;
        public delegate void ResultDelegate(Dictionary<string, UnityEditor.PackageManager.PackageInfo> packageInfoByPackageName);
        ResultDelegate resultCallback;
        public delegate void SingleResultDelegate(UnityEditor.PackageManager.PackageInfo packageInfo);
        SingleResultDelegate singleResultCallback;

        Dictionary<string, UnityEditor.PackageManager.PackageInfo> results = new Dictionary<string, UnityEditor.PackageManager.PackageInfo>();

        public static void LookForPackage(string packageToSearch, SingleResultDelegate packageLookupCallback = null, string defineToAddIfDetected = null)
        {
            var packageCheck = new XRShared.Tools.PackagePresenceCheck(packageToSearch, (packageInfo) => {
                var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

                if (packageInfo != null)
                {
                    if (string.IsNullOrEmpty(defineToAddIfDetected) == false && defines.Contains(defineToAddIfDetected) == false) {
                        defines = $"{defines};{defineToAddIfDetected}";
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                    }
                    packageLookupCallback(packageInfo);
                }
                else
                {
                    if (string.IsNullOrEmpty(defineToAddIfDetected) == false && defines.Contains(defineToAddIfDetected) == true)
                    {
                        Debug.LogError($"Project define symbols include {defineToAddIfDetected}, while package {packageToSearch} is not installed anymore: it should be removed in the Player settings");
                    }
                    packageLookupCallback(null);
                }
            });
        }

        public PackagePresenceCheck(string[] packageNames, ResultDelegate resultCallback, bool useOfflineMode = true)
        {
            this.packageNames = packageNames;
            this.resultCallback = resultCallback;
            request = Client.List(offlineMode: useOfflineMode, includeIndirectDependencies: true);
            EditorApplication.update += Progress;
        }

        public PackagePresenceCheck(string packageName, SingleResultDelegate resultCallback, bool useOfflineMode = true)
        {
            this.packageNames = new string[] { packageName };
            this.singleResultCallback = resultCallback;
            request = Client.List(offlineMode: useOfflineMode, includeIndirectDependencies: true);
            EditorApplication.update += Progress;
        }

        void Progress()
        {
            if (request.IsCompleted)
            {
                bool singleResultCallbackReturned = false;
                results = new Dictionary<string, UnityEditor.PackageManager.PackageInfo>();
                if (request.Status == StatusCode.Success)
                {
                    foreach (var info in request.Result)
                    {
                        foreach(var checkedPackageName in packageNames)
                        {
                            if (info.name == checkedPackageName)
                            {
                                results[checkedPackageName] = info;
                                if (singleResultCallback != null)
                                {
                                    singleResultCallbackReturned = true;
                                    singleResultCallback(info);
                                }
                                break;
                            }
                        }
                    }
                }
                if (singleResultCallback != null && singleResultCallbackReturned == false)
                {
                    singleResultCallback(null);
                }
                if (resultCallback != null)
                    resultCallback(results);
                EditorApplication.update -= Progress;
            }
        }
    }
}
#endif