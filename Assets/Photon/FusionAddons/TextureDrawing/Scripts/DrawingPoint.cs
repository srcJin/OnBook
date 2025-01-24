using Fusion.Addons.DataSyncHelpers;
using UnityEngine;

namespace Fusion.Addons.TextureDrawing
{
    [System.Serializable]
    [ByteArraySize(33)]
    public struct DrawingPoint : RingBuffer.IRingBufferEntry
    {
        public const byte END_DRAW_PRESSURE = 0;

        public Vector2 position;
        public Color color;
        public byte pressureByte;
        public NetworkBehaviourId textureDrawerId;
        // The position of this point in the list of drawn point by this drawer. Used to only display point further than the current interpolation state of the drawer (to avoid displaying point in the "future" of the drawer visual)
        public int positionInDrawerGlobalIndex;

        #region RingBuffer.IRingBufferEntry
        public byte[] AsByteArray
        {
            get
            {
                Vector3 colorData = new Vector3(color.r, color.g, color.b);
                return SerializationTools.AsByteArray(position, colorData, pressureByte, textureDrawerId, positionInDrawerGlobalIndex);
            }
        }

        public void FillFromBytes(byte[] entryBytes)
        {
            int unserializePosition = 0;
            // Position
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out position);
            // Color
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out Vector3 colorData);
            color = new Color(colorData.x, colorData.y, colorData.z, 1);
            // Pressure
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out pressureByte);
            // drawId
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out textureDrawerId);
            // positionInDrawerGlobalIndex
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out positionInDrawerGlobalIndex);
        }
        #endregion
    }

    [System.Serializable]
    [ByteArraySize(29)]
    public struct DrawerDrawingPoint : RingBuffer.IRingBufferEntry
    {
        public const byte END_DRAW_PRESSURE = 0;

        public Vector2 position;
        public Color color;
        public byte pressureByte;
        public NetworkBehaviourId textureDrawingId;

        #region RingBuffer.IRingBufferEntry
        public byte[] AsByteArray
        {
            get
            {
                Vector3 colorData = new Vector3(color.r, color.g, color.b);
                return SerializationTools.AsByteArray(position, colorData, pressureByte, textureDrawingId);
            }
        }

        public void FillFromBytes(byte[] entryBytes)
        {
            int unserializePosition = 0;
            // Position
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out position);
            // Color
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out Vector3 colorData);
            color = new Color(colorData.x, colorData.y, colorData.z, 1);
            // Pressure
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out pressureByte);
            // drawId
            SerializationTools.Unserialize(entryBytes, ref unserializePosition, out textureDrawingId);
        }
        #endregion
    }

}