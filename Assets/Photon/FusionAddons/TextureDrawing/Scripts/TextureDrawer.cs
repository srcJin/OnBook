using Fusion.Addons.DataSyncHelpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.TextureDrawing
{
    /***
     * 
     * Bufferize points to be added to a TextureDrawing.
     * 
     * The `AddDrawingPoint()` method can be called, for instance by the `TexturePen`, to add `DrawingPoint` on a `TextureSurface` through a `TextureDrawing`.
     * It is used to record the drawing points that must be edited on the texture when a contact is detected between the pen and the drawing surface. 
     * When receiving the underlying ring buffer new entries, the new points are shared to the `TextureDrawing`, that will finally apply the changes to the `TextureSurface`
     * 
     ***/
    public class TextureDrawer : RingBufferSyncBehaviour<DrawerDrawingPoint>
    {
        #region Not FUN aligned insertion logic
        [Tooltip("If greater than 0, it will throttle the amount of point added per second")]
        public int maxPointInsertionPerSeconds = 0;
        int maxPointInsertionPerTick = int.MaxValue;
        float delayBeforeNewTransmission;
        float lastTransmission;
        public struct PendingDrawingPoint
        {
            public Vector2 position;
            public Color color;
            public byte pressureByte;
            public TextureDrawing drawing;
            public bool alreadySentToLocalDrawing;
        }
        List<PendingDrawingPoint> pointsToAdd = new List<PendingDrawingPoint>();

        public int LastActuallyStoredIndex => AddedEntryCount - 1;
        public int LastThrottledIndex => LastActuallyStoredIndex + pointsToAdd.Count;
        public int LastInterpolatedDrawIndex => lastInterpolatedDrawIndex;

        [Header("Interpolation")]
        [Tooltip("If true, the drawing will stop displaying point if they were added at a time before the interpolation time")]
        [SerializeField] bool enableInterpolation = true;
        [Tooltip("If true, the interpolation logic will be ignored for the locla user. Relevant when the visual of the drawer also ignore interpolation, like a extrapolated VR hand, always displayed at device position")]
        [SerializeField] bool displayPointImmediatlyForLocalUser = true;
        int lastInterpolatedDrawIndex = -1;

        public override void Spawned()
        {
            base.Spawned();
            DetermineInsertionRate();
        }
        
        public void ForgetTextureDrawingPoints(TextureDrawing textureDrawing)
        {
            ringBuffer.EditEntriesStillInbuffer<DrawerDrawingPoint>(this, (entry) => {
                if (entry.textureDrawingId == textureDrawing.Id)
                {
                    entry.textureDrawingId = NetworkBehaviourId.None;
                    entry.color = Color.clear;
                }
                return entry;
            });
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            InsertThrottledPoints();
        }

        // Called during Spawn
        void DetermineInsertionRate()
        {
            if (maxPointInsertionPerSeconds > 0)
            {
                maxPointInsertionPerTick = Mathf.Max(1, (int)(maxPointInsertionPerSeconds * Runner.DeltaTime));
                delayBeforeNewTransmission = 1f / maxPointInsertionPerSeconds;
            }
        }

        // Ensure to add point to the drawer data, aligned on FUN, and throttled (to limit number of insertion per seconds)
        // For the local user, immediatly adds to the TextureDrawing the point, so that it can be drawn asap
        public void AddPointWithThrottle(Vector2 textureCoord, byte pressure, Color color, TextureDrawing targetDrawing)
        {
            // Send "in advance" the local point to the drawing. It will be immediatly added to the local data cache of the target drawing (OnNewEntries is not called on the local user, so the insertion won't be duplicated)
            int positionInDrawerGlobalIndex = LastThrottledIndex + 1;
            SendDrawingPointDataToLocalDrawing(textureCoord, pressure, color, targetDrawing, positionInDrawerGlobalIndex: positionInDrawerGlobalIndex);

            // Plan to store in the drawer network data the drawing
            pointsToAdd.Add(new PendingDrawingPoint { position = textureCoord, pressureByte = pressure, color = color, drawing = targetDrawing, alreadySentToLocalDrawing = true });
        }

        public void AddStopDrawingPointWithThrottle(TextureDrawing targetDrawing)
        {
            AddPointWithThrottle(Vector2.zero, DrawingPoint.END_DRAW_PRESSURE, Color.clear, targetDrawing);
        }

        // Called during FUN
        void InsertThrottledPoints()
        {
            if (pointsToAdd.Count == 0) return;
            if (maxPointInsertionPerTick == 1 && (Time.time - lastTransmission) < delayBeforeNewTransmission)
            {
                return;
            }
            int addedPoints = 0;
            while (pointsToAdd.Count > 0 && (maxPointInsertionPerTick == 0 || addedPoints < maxPointInsertionPerTick))
            {
                var point = pointsToAdd[0];
                AddDrawingPoint(point.position, point.pressureByte, point.color, point.drawing, sendDatatoLocalDrawing: point.alreadySentToLocalDrawing == false);
                pointsToAdd.RemoveAt(0);
                addedPoints++;
                lastTransmission = Time.time;
            }
        }
        #endregion

        // Should be called during FUN. No throttling to protect the amount of data inserted
        public void AddStopDrawingPoint(TextureDrawing targetDrawing)
        {
            AddDrawingPoint(Vector2.zero, DrawingPoint.END_DRAW_PRESSURE, Color.clear, targetDrawing);
        }

        // Should be called during FUN. No throttling to protect the amount of data inserted
        public void AddDrawingPoint(Vector2 textureCoord, byte pressure, Color color, TextureDrawing targetDrawing, bool sendDatatoLocalDrawing = true)
        {
            if(Object.HasStateAuthority)
            {
                var entry = new DrawerDrawingPoint { 
                    position = textureCoord, 
                    pressureByte = pressure, 
                    color = color, 
                    textureDrawingId = targetDrawing ? targetDrawing.Id : NetworkBehaviourId.None,
                };
                AddEntry(entry);
                // AddEntry does not trigger OnNewEntries for the local user: we have to deal with the new entry directly
                if (sendDatatoLocalDrawing)
                {
                    int lastActuallyStoredIndex = AddedEntryCount - 1;
                    SendDrawingPointDataToLocalDrawing(entry.position, entry.pressureByte, entry.color, targetDrawing, positionInDrawerGlobalIndex: lastActuallyStoredIndex);
                }
            }
            else
            {
                throw new Exception("Should only be called on the state auth of the Drawer - authority change while throttled points were not shared yet ?");
            }
        }

        public void SendDrawingPointDataToLocalDrawing(Vector2 textureCoord, byte pressure, Color color, TextureDrawing targetDrawing, int positionInDrawerGlobalIndex)
        {
            if (targetDrawing == null) return;
            targetDrawing.StoreDrawingPointData(textureCoord, pressure, color, this, positionInDrawerGlobalIndex: positionInDrawerGlobalIndex);
        }

        public override void OnNewEntries(byte[] newPaddingStartBytes, DrawerDrawingPoint[] newEntries, int firstEntryIndex)
        {
            int entryIndex = firstEntryIndex;
            // Note: only called on remotes
            foreach (var entry in newEntries)
            {
                if (Runner.TryFindBehaviour(entry.textureDrawingId, out var drawing))
                {
                    if (drawing is TextureDrawing textureDrawing)
                    {
                        SendDrawingPointDataToLocalDrawing(entry.position, entry.pressureByte, entry.color, textureDrawing, positionInDrawerGlobalIndex: entryIndex);
                    }
                }
                entryIndex++;
            }
        }

        public override void OnDataloss(RingBuffer.LossRange lossRange)
        {
            base.OnDataloss(lossRange);
            if(lossRange.start != 0)
            {
                // If the loss is at 0, it is normal for late joiners, and is handled in the TextureDrawing code
                Debug.LogWarning($"Data loss {lossRange.start} - {lossRange.end}: either increase RingBufferSyncBehaviour.BUFFER_SIZE, or decrease the maximum number of point insertioned per second of the object adding points through AddDrawingPoint (TexturePen, ...)");
            }
        }

        #region Interpolation
        public bool IsInInterpolationRange(int positionInDrawerGlobalIndex)
        {
            return positionInDrawerGlobalIndex <= LastInterpolatedPointIndex;
        }

        public int LastInterpolatedPointIndex
        {
            get
            {
                if (enableInterpolation == false || (displayPointImmediatlyForLocalUser && Object && Object.HasStateAuthority))
                {
                    return LastThrottledIndex;
                }
                else
                {
                    return lastInterpolatedDrawIndex;
                }
            }
        }


        public override void Render()
        {
            base.Render();
            if (TryGetInterpolationTotalEntryCount(out int currentIndex))
            {
                lastInterpolatedDrawIndex = currentIndex;
            }
        }
        #endregion
    }
}
