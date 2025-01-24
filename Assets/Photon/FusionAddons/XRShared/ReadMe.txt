#  XR Shared

## Documentation

https://doc.photonengine.com/fusion/current/industries-samples/industries-addons/fusion-industries-addons-xrshared

## Version & Changelog

- Version 2.0.7:
    - Deal with OnBecameVisible issues on visionOS with Polyspatial
- Version 2.0.6:
    - Add basic feedback component to XRShared 
    - Add SharedDesign folder with common materials and UI elements
    - Add cooldown to PrefabSpawner
    - Add IContactHandler interface
    - Prevent Toucher to trigger callbacks on remote user when used on a NetworkObject (by default, only the state authority of the toucher parent can trigger a toucher)
    - Ungrab when loosing the state authority on a NetworkGrabbable
- Version 2.0.5: 
    - Fix XRControllerInputDevice position when using Oculus XR plug-in
    - Allow to prevent temporarily snap rotation in RigLocomotion
    - Add basic UserSpawner script
    - Add HideRenderers script
- Version 2.0.4: 
    - Prevent disabled Grabber components from grabbing
    - Add reference transforms to the hardware hand: it will be moved at the first active (in hierarchy) transform listed
    - Add WeaverHelper (to check/edit assembliestoWeave in config) and improvement to PackagePresenceCheck
- Version 2.0.3: 
    - Add IColorProvider and IFeedbackHandler interfaces
    - Add GrabbableColorSelection
- Version 2.0.2: 
    - Ensure compatibility with Unity 2021.x (box colliders, edited in 2022.x, in prefab had an improper size when opened in 2021.x)
    - Add layer utils to simplify automatic configuration of layers between projects
- Version 2.0.1: Add VolumeCamera handling in HideForLocalUser if Polyspatial is installed
- Version 2.0.0: First release



