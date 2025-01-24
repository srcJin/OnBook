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
#if OCULUS_SDK_AVAILABLE
using Meta.XR.BuildingBlocks;
using Meta.XR.BuildingBlocks.Editor;
using Meta.XR.MultiplayerBlocks.Shared.Editor;
#endif

namespace Fusion.Addons.HandsSync.Meta
{
    [DefaultExecutionOrder(10_000)]
    [InitializeOnLoad]
    internal static class MetaCoreIntegrationWeaverCheck
    {

        const string PACKAGE_TO_SEARCH = "com.meta.xr.sdk.core";
        const int MIN_VERSION_REQUIRING_BUILDINGBLOCKS_WEAVING = 67;

        private const string FUSION_BB_ASSEMBLY_NAME = "Meta.XR.MultiplayerBlocks.Fusion";

        static MetaCoreIntegrationWeaverCheck()
        { 
            XRShared.Tools.PackagePresenceCheck.LookForPackage(packageToSearch: PACKAGE_TO_SEARCH, packageLookupCallback: (packageInfo) => { 
                if(packageInfo != null)
                {
                    if(string.IsNullOrEmpty(packageInfo.version) != true && Int32.Parse(packageInfo.version.Split('.')[0]) >= MIN_VERSION_REQUIRING_BUILDINGBLOCKS_WEAVING)
                    {
                        FusionWeaverConfigurationCheck();
                    }
                }
            });
        }

        static void FusionWeaverConfigurationCheck()
        {
            const string MISSING_ASSEMBLY_MESSAGE = "The Meta Core SDK code related to Fusion networking building blocks needs to be added to Fusion assemblies to weave";
            if (XRShared.Tools.WeaverHelper.IsInAssembliesToWeave(FUSION_BB_ASSEMBLY_NAME) == false) {
                Debug.LogError($"[Fusion Addons - Meta Core Integration]  {MISSING_ASSEMBLY_MESSAGE}: add {FUSION_BB_ASSEMBLY_NAME} to the network project config, or use MetaXR > Project setup tool view");
            }
#if OCULUS_SDK_AVAILABLE
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Features,
                isDone: _ => XRShared.Tools.WeaverHelper.IsInAssembliesToWeave(FUSION_BB_ASSEMBLY_NAME),
            message:
                MISSING_ASSEMBLY_MESSAGE,
                fix: _ =>
                {
                    XRShared.Tools.WeaverHelper.AddToAssembliesToWeave(FUSION_BB_ASSEMBLY_NAME);
                },
                fixMessage: $"Add blocks assembly {FUSION_BB_ASSEMBLY_NAME} to Fusion project config's AssembliesToWeave"
            );
#endif
        }
    }
}

#endif // FUSION_WEAVER && FUSION2
