using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.HandsSync

{
    /*
     * Follow the index tip position provided by a HardwareHandRepresentationManager
     * 
     * Usefull to have a separate index based collider, for touch only, or other action that would be less accurate if impacted by the whole hnad collider used for grabbing
     */
    public class IndexTipFollower : MonoBehaviour
    {
        [SerializeField]
        HardwareHandRepresentationManager hardwareHandRepresentationManager;

        private void Awake()
        {
            if(hardwareHandRepresentationManager == null)
            {
                hardwareHandRepresentationManager = GetComponentInParent<HardwareHandRepresentationManager>();
            }
            if(hardwareHandRepresentationManager == null)
            {
                Debug.LogError("Missing hardwareHandRepresentationManager");
            }
        }

        private void LateUpdate()
        {
            var indexTiptransform = hardwareHandRepresentationManager.IndexTip;
            if (indexTiptransform)
            {
                transform.position = indexTiptransform.position;
                transform.rotation = indexTiptransform.rotation;
            }
        }
    }

}
