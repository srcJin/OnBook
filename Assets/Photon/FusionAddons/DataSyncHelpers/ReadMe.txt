# DataSyncHelper

## Documentation

https://doc.photonengine.com/fusion/current/industries-samples/industries-addons/fusion-industries-addons-datasynchelpers


## Version & Changelog

- Version 2.0.8:
  - Add ByteArraySize attribute for IByteArraySerializable struct, to provide a way to explicitly give the serialization size of a IByteArraySerializable
  - Give hint on how to fill the ByteArraySize size value, if not provided
  - Add RingBuffer.EditEntriesStillInbuffer to have a callback allowing to edit the latest entries still stored in the buffer
- Version 2.0.7:
  - Add in StreamSynchedBehaviour a way to clear data on all  users
- Version 2.0.6:
  - Add RingBufferLosslessSyncBehaviour.TryGetInterpolationTotalEntryCount, to find the entry count interpolated between each interpolation state entry count (from and to)
- Version 2.0.5:
  - Add RingBuffer.PositionInfo to simplify finding the current position in a from/to buffer
- Version 2.0.4: 
  - Add sample drawing code
- Version 2.0.3: 
  - Allow demo CameraPicture to forward to a specific RenderTexture
  - Add solutions (StreamingAPIConfiguration and overridable TargetId methods) if a NetworkObject contains 2 components using streaming API at the same time 
- Version 2.0.2: Add support for NetworkBehaviourId serialization in tools
- Version 2.0.1: 
 - Add Vector2 and uint support in serialization tools
 - add flexibility to ring buffer entry size determination
- Version 2.0.0: First release 
