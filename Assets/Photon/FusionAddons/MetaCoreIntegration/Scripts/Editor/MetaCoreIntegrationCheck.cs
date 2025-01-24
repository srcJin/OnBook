using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System;


#if FUSION_WEAVER && FUSION2
using System.Linq;
using Fusion;
using Fusion.Editor;
using UnityEditor;

namespace Fusion.Addons.HandsSync.Meta
{
    [InitializeOnLoad]
    internal static class MetaCoreIntegrationCheck
    {

        public const string PACKAGE_TO_SEARCH = "com.meta.xr.sdk.core";
        //public const string PACKAGE_TO_INSTALL = "com.meta.xr.sdk.core";
        public const string DEFINE = "OCULUS_SDK_AVAILABLE";
        const bool DISPLAY_ERROR_IF_MISSING = false;

        public const string INTERACTION_PACKAGE_TO_SEARCH = "com.meta.xr.sdk.interaction.ovr";
        public const string INTERACTION_DEFINE = "OCULUS_INTERACTION_SDK_AVAILABLE";
        static MetaCoreIntegrationCheck()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.VisionOS)
            {
                // Meta Core SDK is incompatible with visionOs build, for now: not adding the define check to allow scripts to remove Meta packages if needed
                return;
            }
            XRShared.Tools.PackagePresenceCheck.LookForPackage(packageToSearch: PACKAGE_TO_SEARCH, defineToAddIfDetected: DEFINE, packageLookupCallback: (packageInfo) => {
                if (packageInfo == null)
                {
                    if (DISPLAY_ERROR_IF_MISSING)
                    {
#pragma warning disable CS0162 // Unreachable code detected
                        Debug.LogError($"[Fusion Addons - Meta Core Integration] For the Meta integration to work, you need to install the package {PACKAGE_TO_SEARCH}\n" +
                        $"See https://developer.oculus.com/documentation/unity/unity-package-manager/");
#pragma warning restore CS0162 // Unreachable code detected
                    }
                    //Client.Add(PACKAGE_TO_INSTALL);
#if OCULUS_SDK_AVAILABLE
#pragma warning disable CS0162 // Unreachable code detected
                    Debug.LogError("[Fusion Addons - Meta Core Integration] Meta Core SDK package not installed, but OCULUS_SDK_AVAILABLE still available in Player settings define symbols: remove it to avoid error while the Meta package is not installed");
#pragma warning restore CS0162 // Unreachable code detected
#endif 
                }
            });

            // Interaction SDK
            XRShared.Tools.PackagePresenceCheck.LookForPackage(packageToSearch: INTERACTION_PACKAGE_TO_SEARCH, defineToAddIfDetected: INTERACTION_DEFINE, packageLookupCallback: (packageInfo) => {});
        }
    }
}

#endif // FUSION_WEAVER && FUSION2
