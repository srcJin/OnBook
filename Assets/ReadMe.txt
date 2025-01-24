# Sticky Notes Meta SSA Colocation

## Documentation

https://doc.photonengine.com/arvr/current/meta-quest/fusion-stickynote-meta-ssa-colocation


## Version & Changelog

- Version 1.2:
	- Add feature to clear sitcky note content [TextureDrawing-2.1.2] [DataSyncHelpers-2.0.8]
	- Changed DataSyncHelpers to allow editing data stored in a Ringbuffer  [DataSyncHelpers-2.0.8]
	- Add a ByteArraySize attribute to reduce allocations [DataSyncHelpers-2.0.8]/[LineDrawing-2.0.5]/[TextureDrawing-2.1.2]
	- Fix UI issues
	- Add haptic feedback on touch drawing

- Version 1.1:
	- Fix SkinnedMeshRenderer animation culling mode on visionOS [VisionOSHelper-2.0.7]
	- Fix dependencies check [MXInkIntegration-2.0.2]
	- Deal with OnBecameVisible issues on visionOS with Polyspatial [XR Shared-2.0.7]
	- Provide haptic feedback during 3D draw [Line Drawing-2.0.4]

- Version 1.0: First release