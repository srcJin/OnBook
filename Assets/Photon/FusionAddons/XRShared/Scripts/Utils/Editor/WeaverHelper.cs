#if UNITY_EDITOR
using Fusion.Editor;
using System;


namespace Fusion.XRShared.Tools
{
    public class WeaverHelper
    {
        public static bool IsInAssembliesToWeave(string assemblyName)
        {
            if (NetworkProjectConfigAsset.TryGetGlobal(out var global))
            {
                var config = global.Config;
                return Array.IndexOf(config.AssembliesToWeave, assemblyName) >= 0;
            }
            return false;
        }

        public static void AddToAssembliesToWeave(string assemblyName)
        {
            if (NetworkProjectConfigAsset.TryGetGlobal(out var global))
            {
                var config = global.Config;
                string[] current = config.AssembliesToWeave;
                if (Array.IndexOf(current, assemblyName) < 0)
                {
                    config.AssembliesToWeave = new string[current.Length + 1];
                    for (int i = 0; i < current.Length; i++)
                    {
                        config.AssembliesToWeave[i] = current[i];
                    }
                    config.AssembliesToWeave[current.Length] = assemblyName;
                    NetworkProjectConfigUtilities.SaveGlobalConfig();
                }
            }
        }
    }
}
#endif 