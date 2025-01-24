using Fusion;
using Fusion.Addon.Colocalization;
using Fusion.XR.Shared.Rig;
#if OCULUS_SDK_AVAILABLE
using Meta.XR.MRUtilityKit;
#endif
using UnityEngine;

/***
 * 
 * The SpaceLocalizationHandler class is used to handle the localization of the space. 
 * The ReferenceWallSelection() method is called when the user selects the main wall : 
 *          - it creates the networked reference wall (anchor) if it has been spawned by a previous player.
 *          - or it teleport the user to a new position based on the networked reference anchor position and the local wall position.
 * 
 ***/
public class SpaceLocalizationHandler : MonoBehaviour
{
    HardwareRig hardwareRig;
    ReferenceWall referenceWall;
    NetworkRunner runner;
    public NetworkObject referenceWallPrefab;

    bool aimReferenceWallAxisAtUser = true;

    [ContextMenu("ReferenceWallSelection")]
    public void ReferenceWallSelection()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("Runner not found");
            return;
        }

        // Check if a reference wall has been spawned by a previous player
        DetectReferenceWall();

        if (referenceWall)
        {
            // Teleport the user
            TeleportUserToPlaceReferenceAnchoreAtSelectedRealLifeposition();
        }
        else
        {
            // Compute the reference anchor and spawn it
            var referenceWallRotation = transform.rotation;
            if (aimReferenceWallAxisAtUser)
            {
                if (hardwareRig == null)
                {
                    hardwareRig = FindObjectOfType<HardwareRig>();
                }
                var vectorToHead = hardwareRig.headset.transform.position - transform.position;

                var newForward = Vector3.Project(vectorToHead, transform.forward);
                var newUpCandidate1 = Vector3.Project(hardwareRig.transform.up, transform.up);
                var newUpCandidate2 = Vector3.Project(hardwareRig.transform.up, transform.right);
                var newUp = newUpCandidate1.magnitude > newUpCandidate2.magnitude ? newUpCandidate1 : newUpCandidate2;
                referenceWallRotation = Quaternion.LookRotation(newForward, newUp);
            }
            runner.Spawn(referenceWallPrefab, transform.position, referenceWallRotation);
        }
    }
    void DetectReferenceWall()
    {
        referenceWall = FindObjectOfType<ReferenceWall>();
    }

    async void TeleportUserToPlaceReferenceAnchoreAtSelectedRealLifeposition()
    {
        if (hardwareRig == null)
        {
            hardwareRig = FindObjectOfType<HardwareRig>();
        }
        var currentWallRotation = transform.rotation;
        if (aimReferenceWallAxisAtUser)
        {
            var vectorToHead = hardwareRig.headset.transform.position - transform.position;

            var newForward = Vector3.Project(vectorToHead, transform.forward);
            var newUpCandidate1 = Vector3.Project(hardwareRig.transform.up, transform.up);
            var newUpCandidate2 = Vector3.Project(hardwareRig.transform.up, transform.right);
            var newUp = newUpCandidate1.magnitude > newUpCandidate2.magnitude ? newUpCandidate1 : newUpCandidate2;
            currentWallRotation = Quaternion.LookRotation(newForward, newUp);
        }

        Vector3 localAnchorPosition = transform.position;
        Quaternion localAnchorRotation = currentWallRotation;
        var targetAnchor = referenceWall.transform;

        // Find the new rig position/rotation to make targetAnchor appear in real life at the position of localAnchor
        (var newRigPosition, var newRigRotation) =
            MixedRealityRelocalization.NewRigPositionToMoveAnchorToTarget(localAnchorPosition, localAnchorRotation, 
            targetAnchor.position, targetAnchor.rotation, 
            hardwareRig.transform, hardwareRig.headset.transform, 
            ignoreYAxisMove: true);

#if OCULUS_SDK_AVAILABLE
        // We must disable EnableWorldLock to allow Meta Rig teleport
        if (MRUK.Instance != null)
        {
            MRUK.Instance.EnableWorldLock = false;
        }
#endif
        hardwareRig.transform.SetPositionAndRotation(newRigPosition, newRigRotation);

        Debug.Log("TeleportToPlaceBeaconAtCurrentRealLifeposition Done "+ newRigPosition);

#if OCULUS_SDK_AVAILABLE
        // Reload the scene and enable the EnableWorldLock parameter after the rig teleport
        if (MRUK.Instance != null)
        {
            MRUK.Instance.ClearScene();
            MRUK.Instance.EnableWorldLock = true;
            await MRUK.Instance.LoadSceneFromDevice();
            WallVisibilityHandler.TriggerGlobalOnMainWallSelected();
        }
#endif
    }
}
