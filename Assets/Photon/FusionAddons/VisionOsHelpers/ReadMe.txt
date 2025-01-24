# VisionOS Helpers

## Documentation

https://doc.photonengine.com/fusion/current/industries-samples/industries-addons/fusion-industries-addons-visionoshelpers

## Version & Changelog

- Version 2.0.7: Fix - SkinnedMeshRenderer needs animator culling mode to be set to always animate on visionOs 
- Version 2.0.6:
  - Add VolumeCameraLocomotion to follow HardwareRig movements
  - Add a SpatialPointerState parameter to ISpatialTouchListener (breaking change)
- Version 2.0.5: 
  - Add preventGrabbingSpatialTouchListeners option to allow or not spatial touching a grabbable
  - Add option in VisionOSHandsConfiguration to hide hands
- Version 2.0.4: compatibility with XRShared 2.0.2
- Version 2.0.3: 
    - Fix some cases where LineMesh points could not appear (normal improperly computed on the path)
    - Add namespace & comments
- Version 2.0.2: Support direct pinch grabbing on SpatialTouchHandler (hardwareRigGrabberDisabled option, enabled by default), and multi spatial grabbing
- Version 2.0.1: Add define checks (to handle when Fusion not yet installed)
- Version 2.0.0: First release
