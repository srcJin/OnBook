// Uncomment if Avatar addon is installed
//#define AVATAR_ADDON_AVAILABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.HandsSync
{
#if AVATAR_ADDON_AVAILABLE
    [RequireComponent(typeof(HandRepresentationManager))]
    public class HandManagerAvatarRepresentationConnector : MonoBehaviour, IAvatarRepresentationListener
    {
        HandRepresentationManager handRepresentationManager;

        void Awake()
        {
            handRepresentationManager = GetComponent<HandRepresentationManager>();
        }

    #region IAvatarRepresentationListener
        public void OnRepresentationAvailable(IAvatar avatar, bool isLocalUserAvatar)
        {
            if (avatar.AvatarDescription.colorMode == AvatarDescription.ColorMode.Color)
            {
                handRepresentationManager.ChangeHandColor(avatar.AvatarDescription.skinColor);
            }
            else if (avatar.AvatarDescription.colorMode == AvatarDescription.ColorMode.Material)
            {
                handRepresentationManager.ChangeHandMaterial(avatar.AvatarDescription.skinMaterial);
            }
        }

        public void OnAvailableAvatarsListed(AvatarRepresentation avatarRepresentation) { }
        public void OnRepresentationUnavailable(IAvatar avatar) { }
    #endregion
    }
#else
    public class HandManagerAvatarRepresentationConnector : MonoBehaviour {
        private void Awake()
        {
            Debug.LogError("Error: Uncomment #define AVATAR_ADDON_AVAILABLE if avatar addon is used to be able to use this component");
        }
    }
#endif
}
