using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.ExtendedRigSelectionAddon
{
    [DefaultExecutionOrder (-1000)]
    public class PlatformSpecificMaterial : MonoBehaviour
    {
        [System.Serializable]
        public struct MaterialSetting
        {
            public bool enableMesh;
            public Material material;
            public RuntimePlatform platform;
        }

        public List<MaterialSetting> materialSettings = new List<MaterialSetting>();

        private void Awake()
        {
            foreach (var setting in materialSettings)
            {
                if (setting.platform == Application.platform)
                {
                    if(TryGetComponent<MeshRenderer>(out var mesh))
                    {
                        mesh.enabled = setting.enableMesh;
                    }

                    if (TryGetComponent<Renderer>(out var renderer))
                    {
                        renderer.material = setting.material;
                    }

                }
            }
        }
    }
}
