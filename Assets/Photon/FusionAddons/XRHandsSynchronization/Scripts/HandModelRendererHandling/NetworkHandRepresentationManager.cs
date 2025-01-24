using UnityEngine;
using Fusion.XR.Shared.Rig;

namespace Fusion.Addons.HandsSync
{
    /***
     * 
     * NetworkHandRepresentationManager component is located on players network rig.
     * It manages the local hand representation according to the displayForLocalPlayer bool,
     * (most of the time, hardware hands and network hands should not be displayed in the same time).
     * 
     ***/
    public class NetworkHandRepresentationManager : HandRepresentationManager
    {
        [SerializeField]
        NetworkBonesStateSync networkBonesStateSync;
        public NetworkHand networkHand;

        public IHandRepresentation handRepresentation;

        [Tooltip("Allow to ignore tracking state from networkBonesStateSync (for instance if hand rendering should be disabled due to something else)")]
        public bool forceDisableHandTracking = false;

        [SerializeField] Material materialForLocalUser = null;

#region HandRepresentationManager implementations
        public override HandTrackingMode CurrentHandTrackingMode {
            get {
                if (forceDisableHandTracking)
                {
                    return HandTrackingMode.NotTracked;
                }
                var mode = (networkBonesStateSync && networkBonesStateSync.Object) ? networkBonesStateSync.CurrentHandTrackingMode : HandTrackingMode.NotTracked;
                return mode;
            }
        }
        public override RigPart Side => networkHand.side;
        public override GameObject FingerTrackingHandSkeletonParentGameObject => networkBonesStateSync.gameObject;
        public override GameObject ControllerTrackingHandSkeletonParentGameObject => gameObject;
#endregion

        protected override void Awake()
        {
            base.Awake();
            if (handRepresentation == null) handRepresentation = GetComponentInChildren<IHandRepresentation>();
            if (networkBonesStateSync == null) networkBonesStateSync = GetComponentInChildren<NetworkBonesStateSync>();
            if (networkHand == null) networkHand = GetComponentInParent<NetworkHand>();
        }

        bool initialStateDetected = false;

        protected override void Update()
        {
            base.Update();
            if (initialStateDetected == false)
            {
                if (networkBonesStateSync && networkBonesStateSync.Object )
                {
                    if (networkBonesStateSync.IsLocalPlayer && materialForLocalUser != null )
                    {
                        ChangeRendererMaterial(fingerTrackingHandMeshRenderer, materialForLocalUser);
                    }
                    initialStateDetected = true;
                }
            }
        }


        protected override void ChangeRendererMaterial(Renderer renderer, Material material)
        {
            if (renderer && networkBonesStateSync && networkBonesStateSync.IsLocalPlayer && materialForLocalUser != null && renderer == fingerTrackingHandMeshRenderer )
            {
                renderer.material = materialForLocalUser;
            }
            else
            {
                base.ChangeRendererMaterial(renderer, material);
            }
        }
    }
}
