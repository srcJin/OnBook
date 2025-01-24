using Fusion.Addons.DataSyncHelpers;
using Fusion.XR.Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.TextureDrawing
{

    /***
     * This component contains all the `DrawingPoint` of the texture that have been added, in the referenced `TextureSurface`.
     * When new entries are added by the `TextureDrawer`, on all clients, the `Draw()` method updates the `TextureSurface` to add a point or draw a line. 
     *
     * Thanks to this StreamSynchedBehaviour mechanism, tracking the expected TotalDataLength, late joiners will receive all the points that need to be drawn on the `TextureSurface` when they join the room.
     * They are then merged with points already received (late joining data usually take a bit more time to arrive)
     * 
     * The class stores the latest point drawn for each `TextureDrawer`, to create lines between the previous point added and the latest one.
     * 
     * It calls the `OnRedrawRequired` method when the `onRedrawRequired` event of the `TextureSurface` occurs (when merge late joining data, or when an external component triggers it)
     ***/
    public class TextureDrawing : StreamSynchedBehaviour
    {
        // Complete cache
        Dictionary<NetworkBehaviourId, List<DrawingPoint>> drawingPointsByDrawerId = new Dictionary<NetworkBehaviourId, List<DrawingPoint>>();
        // Last drawn point index
        Dictionary<NetworkBehaviourId, int> lastDrawnPointIndexByDrawerId = new Dictionary<NetworkBehaviourId, int>();
        Dictionary<NetworkBehaviourId, TextureDrawer> drawerByDrawerId = new Dictionary<NetworkBehaviourId, TextureDrawer>();
        public TextureSurface textureSurface;

        [Header("Drawer color override")]
        public bool overrideDrawerColor = false;
        public Color overrideColor = Color.black;

        public static List<TextureDrawing> TextureDrawingRequestingToBeDrawn = new List<TextureDrawing>();

        int globalFrameLineDrawingBudget = 20;
        int remainingFrameLineDrawingBudgetPerDrawing = 0;
        int frameLineDrawingBudgetPerDrawing = 0;
        static int RemainingGlobalFrameLineDrawingBudget = 0;
        int remainingFrameLineDrawingBudgetPerDrawer = 0;

        [Header("Debug")]
        [SerializeField] bool displayDebugLogs = false;

        
        private void Awake()
        {
            if (textureSurface == null) textureSurface = GetComponent<TextureSurface>();
            textureSurface.onRedrawRequired.AddListener(OnRedrawRequired);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            textureSurface.onRedrawRequired.RemoveListener(OnRedrawRequired);
        }
        
        private void LateUpdate()
        {
            if (drawingInterrupted)
            {
                QueueInDrawingWaitingList(frameDrawnPointsCount);
            }
            DetermineDrawingBudgets();
        }

        #region Texture surface callback
        void OnRedrawRequired()
        {
            // An external component manually edited the texture: we redraw the full drawing
            RedrawFullDrawing();
        }
        #endregion

        TextureDrawer FindDrawer(NetworkBehaviourId drawerId)
        {
            if (drawerByDrawerId.ContainsKey(drawerId) && drawerByDrawerId[drawerId] != null)
            {
                return drawerByDrawerId[drawerId];
            }
            if (Runner.TryFindBehaviour<TextureDrawer>(drawerId, out var drawer))
            {
                drawerByDrawerId[drawerId] = drawer;
                return drawer;
            }
            return null;
        }

        #region Data handling
        // Add a data point. Will be progressively drawn during Render (taking into account the drawer interpolation state)
        public void StoreDrawingPointData(Vector2 position, byte pressureByte, Color color, TextureDrawer textureDrawer, int positionInDrawerGlobalIndex)
        {
            if (overrideDrawerColor)
            {
                color = overrideColor;
            }

            var entry = new DrawingPoint
            {
                position = position,
                pressureByte = pressureByte,
                color = color,
                textureDrawerId = textureDrawer.Id,
                positionInDrawerGlobalIndex = positionInDrawerGlobalIndex
            };

            // Store data as DrawingPoint, easier to be manipulated to actual interpolate drawing
            AddLocalEntry(entry);

            // Store data in data cache, to share with late joiners or to redraw fully
            // Note: as an alternative implementation, cached data, sent to late joiner, could also not include positionInDrawerGlobalIndex, which is used for interpolation purposes, not relevant for late joiner resync (the data will be too old for interpolation to matter)
            AddLocalData(entry.AsByteArray);
        }

        // Store entry in local drawingPointsByDrawerId[entry.referenceId] cache
        void AddLocalEntry(DrawingPoint entry)
        {
            if (drawingPointsByDrawerId.ContainsKey(entry.textureDrawerId) == false)
            {
                drawingPointsByDrawerId[entry.textureDrawerId] = new List<DrawingPoint>();
            }
            drawingPointsByDrawerId[entry.textureDrawerId].Add(entry);
        }

        void RedrawFullDrawing() {
            DrawingPoint[] entriesArray = SplitCompleteData<DrawingPoint>();

            // We restart the drawing: there should be no "previous" drawing points
            lastDrawnPointIndexByDrawerId.Clear();
            drawingPointsByDrawerId.Clear();

            var points = new List<DrawingPoint>(entriesArray);
            foreach (var entry in points)
            {
                AddLocalEntry(entry);
            }
        }
        #endregion

        #region Drawing
        public override void Render()
        {
            base.Render();

            DrawPendingPoints();
        }

        int PointRequiringDrawingforDrawerId(NetworkBehaviourId drawerId)
        {
            int maxIndexToDraw = MaxIndexToDrawForDrawerId(drawerId);
            return PointRequiringDrawingforDrawerId(drawerId, maxIndexToDraw);
        }

        int PointRequiringDrawingforDrawerId(NetworkBehaviourId drawerId, int maxIndexToDraw)
        {
            if(maxIndexToDraw == -1)
            {
                return 0;
            }

            var lastDrawn = LastDrawnIndexForDrawerId(drawerId);
            if (lastDrawn != -1)
            {
                return maxIndexToDraw - lastDrawn;
            }
            else
            {
                // Nothing drawn yet: everything requires to be drawn
                return maxIndexToDraw;
            }
        }

        int MaxIndexToDrawForDrawerId(NetworkBehaviourId drawerId)
        {
            TextureDrawer drawer = FindDrawer(drawerId);
            int maxIndex = -1;
            if (drawer && drawer.Id != NetworkBehaviourId.None)
            {
                // Drawer available - we'll use its interpolation state to determine up to which point we should draw
                if (drawer == null || drawer.Id == NetworkBehaviourId.None) throw new Exception("Unexpected state");

                if (drawer.LastInterpolatedPointIndex < 0)
                {
                    return -1;
                }
                if (drawingPointsByDrawerId.ContainsKey(drawer.Id) == false)
                {
                    return -1;
                }
                var points = drawingPointsByDrawerId[drawer.Id];

                var maxIndexToDraw = points.Count - 1;
                var lastDrawerInterpolatedPointIndex = drawer.LastInterpolatedPointIndex;
                // look for the latest point above the interpolated max drawing position count of the drawer
                while (maxIndexToDraw > 0 && points[maxIndexToDraw].positionInDrawerGlobalIndex > lastDrawerInterpolatedPointIndex)
                {
                    maxIndexToDraw--;
                }
                return maxIndexToDraw;
            }
            else if (drawingPointsByDrawerId.ContainsKey(drawerId))
            {
                // Destroyed or offline browser: no interpolation state to look for
                var points = drawingPointsByDrawerId[drawerId];
                if(points.Count > 0)
                {
                    maxIndex = points.Count - 1;
                }
            }
            return maxIndex;
        }

        int LastDrawnIndexForDrawerId(NetworkBehaviourId drawerId)
        {
            var lastDrawnIndex = -1;
            if (lastDrawnPointIndexByDrawerId.ContainsKey(drawerId))
            {
                lastDrawnIndex = lastDrawnPointIndexByDrawerId[drawerId];
            }
            return lastDrawnIndex;
        }

        List<NetworkBehaviourId> drawerIdRequiringDrawing = new List<NetworkBehaviourId>();
        int frameDrawnPointsCount = 0;
        bool drawingInterrupted = false;

        void DrawPendingPoints() { 
            frameDrawnPointsCount = 0;

            drawerIdRequiringDrawing.Clear();

            // Find the drawer requiring to be drawn, to share the drawing budget (amount of line draws allowed per frame) among them
            foreach (var drawerId in drawingPointsByDrawerId.Keys)
            {
                if (PointRequiringDrawingforDrawerId(drawerId) > 0)
                {
                    drawerIdRequiringDrawing.Add(drawerId);
                }
            }

            // Determine drawing budget per drawer
            drawingInterrupted = false;
            var budgetPerDrawer = remainingFrameLineDrawingBudgetPerDrawing;
            if(drawerIdRequiringDrawing.Count > 1)
            {
                budgetPerDrawer = Mathf.Max(1, remainingFrameLineDrawingBudgetPerDrawing / drawerIdRequiringDrawing.Count);
            }

            foreach (var drawerId in drawerIdRequiringDrawing)
            {
                int drawnforDrawer = 0;
                bool drawingInterruptedForDrawer = false;
                TextureDrawer drawer = FindDrawer(drawerId);
                remainingFrameLineDrawingBudgetPerDrawer = budgetPerDrawer;
                var lastDrawnIndex = LastDrawnIndexForDrawerId(drawerId);
                var points = drawingPointsByDrawerId[drawerId];
                int maxIndexToDraw = MaxIndexToDrawForDrawerId(drawerId);

                while (lastDrawnIndex < (points.Count - 1))
                {
                    var drawingIndex = lastDrawnIndex + 1;

                    // Interpolation
                    if (drawer && drawer.IsInInterpolationRange(points[drawingIndex].positionInDrawerGlobalIndex) == false)
                    {
                        // Not already here in the inteprolated points - no need to draw
                        if (displayDebugLogs)
                        {
                            Debug.LogError($"Draw only {lastDrawnIndex}/{points.Count - 1}, as drawer interpolation is only at {drawer.LastInterpolatedDrawIndex}/{drawer.AddedEntryCount} ({points[drawingIndex].positionInDrawerGlobalIndex} requested)");
                        }
                        break;
                    }

                    // Drawing budget
                    bool canBeDrawn = CanBeDrawn;

                    if (canBeDrawn == false)
                    {
                        // Total drawing budget spent: adding ourselves to the waiting list (in the next Lastupdate), to preserve performances
                        drawingInterruptedForDrawer = true;
                        break;
                    }

                    lastDrawnIndex = drawingIndex;

                    // Actual draw
                    Draw(points[drawingIndex]);

                    lastDrawnPointIndexByDrawerId[drawerId] = lastDrawnIndex;
                    frameDrawnPointsCount++;
                    drawnforDrawer++;
                }
                if (displayDebugLogs)
                {
                    if (drawingInterruptedForDrawer)
                    {
                        Debug.LogError($"[drawing:{Id}] INTERRUPTED Drawing for drawer:{drawerId}. Drawn: {drawnforDrawer}({frameDrawnPointsCount}). Indexes: {lastDrawnIndex}/{points.Count - 1} (target index: {maxIndexToDraw} / remain: {PointRequiringDrawingforDrawerId(drawerId, maxIndexToDraw)})");
                    }
                    else
                    {
                        Debug.LogError($"*** [drawing:{Id}] FINISHED Drawing  for {drawerId}. Drawn: {drawnforDrawer}({frameDrawnPointsCount}). Indexes: {lastDrawnIndex}/{points.Count - 1} (target index: {maxIndexToDraw} / remain: {PointRequiringDrawingforDrawerId(drawerId, maxIndexToDraw)}) ***");
                    }
                }
                drawingInterrupted = drawingInterrupted || drawingInterruptedForDrawer;
            }
            if (drawingInterrupted == false)
            {
                // Drawing completed, removing form the waiting list 
                RemoveFromDrawingWaitingList();
            }
            //if (frameDrawnPointsCount > 0) Debug.LogError($"[{Id}/{TextureDrawingRequestingToBeDrawn.IndexOf(this)}] frameDrawnPointsCount: {frameDrawnPointsCount}/RemainingGlobalFrameLineDrawingBudget:{RemainingGlobalFrameLineDrawingBudget}");
        }

        
        public void Draw(DrawingPoint point)
        {
            // Determine if it is the first point of a line (either the first point of for this drawer, of if the last point had a end of line DrawingPoint.END_DRAW_PRESSURE pressure),
            //  or if we should draw a line from the last point to the new one
            DrawingPoint lastPoint = new DrawingPoint();
            bool hasLastPoint = false;
            if (lastDrawnPointIndexByDrawerId.ContainsKey(point.textureDrawerId))
            {
                if (drawingPointsByDrawerId.ContainsKey(point.textureDrawerId) == false && drawingPointsByDrawerId[point.textureDrawerId].Count < lastDrawnPointIndexByDrawerId[point.textureDrawerId])
                {
                    throw new Exception("Incorrect drawing point cached data");
                }
                lastPoint = drawingPointsByDrawerId[point.textureDrawerId][lastDrawnPointIndexByDrawerId[point.textureDrawerId]];
                // If last "point" is an endline, then there is no last point
                hasLastPoint = lastPoint.pressureByte != DrawingPoint.END_DRAW_PRESSURE;
            }

            if (point.pressureByte == DrawingPoint.END_DRAW_PRESSURE)
            {
                // We do not draw an endline
            }
            else if (hasLastPoint == false)
            {
                textureSurface.AddPoint(point.position, point.pressureByte, point.color);
            }
            else
            {
                textureSurface.AddLine(lastPoint.position, lastPoint.pressureByte, lastPoint.color, point.position, point.pressureByte, point.color);
            }
            RemainingGlobalFrameLineDrawingBudget--;
            remainingFrameLineDrawingBudgetPerDrawing--;
            remainingFrameLineDrawingBudgetPerDrawer--;
        }
        #endregion

        #region Drawing Budget (for performances control)
        private void RemoveFromDrawingWaitingList()
        {
            if (TextureDrawingRequestingToBeDrawn.Contains(this))
            {
                // Drawing completed, removing form the waiting list 
                TextureDrawingRequestingToBeDrawn.Remove(this);
            }
        }
        private void QueueInDrawingWaitingList(int frameDrawnPointsCount)
        {
            //Debug.Log($"[{Id}/{TextureDrawingRequestingToBeDrawn.IndexOf(this)}] Pausing drawing for performances {lastDrawnIndex+1}/{points.Count}. lastFrameDrawnPointsCount:{frameDrawnPointsCount}/remainingFrameBudgetPerDrawing:{remainingFrameLineDrawingBudgetPerDrawing}/RemainingGlobalFrameLineDrawingBudget:{RemainingGlobalFrameLineDrawingBudget}/WAit list count:{TextureDrawingRequestingToBeDrawn.Count}");
            if (TextureDrawingRequestingToBeDrawn.Contains(this) == false)
            {
                TextureDrawingRequestingToBeDrawn.Add(this);
            }
            else if (frameDrawnPointsCount > 0 && TextureDrawingRequestingToBeDrawn.Count > 1)
            {
                // We had the opportunity to draw, and other people are waiting, move to the back of the queue list
                TextureDrawingRequestingToBeDrawn.Remove(this);
                TextureDrawingRequestingToBeDrawn.Add(this);
            }
        }

        bool CanBeDrawn => RemainingGlobalFrameLineDrawingBudget > 0 && remainingFrameLineDrawingBudgetPerDrawing > 0 && remainingFrameLineDrawingBudgetPerDrawer > 0;

        void DetermineDrawingBudgets()
        {
            if (TextureDrawingRequestingToBeDrawn.Count > 1)
            {
                frameLineDrawingBudgetPerDrawing = Mathf.Max(1, globalFrameLineDrawingBudget / TextureDrawingRequestingToBeDrawn.Count);
            }
            else
            {
                frameLineDrawingBudgetPerDrawing = globalFrameLineDrawingBudget;
            }
            remainingFrameLineDrawingBudgetPerDrawing = frameLineDrawingBudgetPerDrawing;
            RemainingGlobalFrameLineDrawingBudget = globalFrameLineDrawingBudget;
        }
        #endregion

        #region StreamSynchedBehaviour    
        public override void Send(byte[] data)
        {
            // We replace here the logic of StreamSynchBehaviour: it is not used to send directly data, then recovered by late joiner, but to store local data (coming from TextureDrawer), and then sharing them with late joiner
        }

        // Called upon reception of the cache of existing point, through the streaming API, for late joiners
        // We override the OnDataChunkReceived (that will be used here only as a late joiner)
        //  to avoid storing the data received locally at the start as it does usually (since we want the full cache to be at the start of the storage in our case)
        public override void OnDataChunkReceived(byte[] newData, PlayerRef source, float time)
        {
            if (newData.Length == 0) return;

            // Convert data bytes to drawing point entries
            ByteArrayTools.Split<DrawingPoint>(newData, out var newPaddingStartBytes, out var points);
            
            // We find the drawers for which their last line is not finished in the received data chunk
            List<NetworkBehaviourId> drawerIds = new List<NetworkBehaviourId>();
            foreach(var point in points)
            {
                if(drawerIds.Contains(point.textureDrawerId) == false && point.pressureByte != DrawingPoint.END_DRAW_PRESSURE)
                {
                    drawerIds.Add(point.textureDrawerId);
                }
                else if (drawerIds.Contains(point.textureDrawerId) && point.pressureByte == DrawingPoint.END_DRAW_PRESSURE)
                {
                    drawerIds.Remove(point.textureDrawerId);
                }
            }

            foreach(var drawerId in drawerIds)
            {
                // We inject endlines points, between the existing points and the full cache received from already connected players, as some of the already received data might be also in the full data cache - a better implementation would be to drop the data in double
                var entry = new DrawingPoint
                {
                    pressureByte = DrawingPoint.END_DRAW_PRESSURE,
                    textureDrawerId = drawerId
                };
                InsertLocalDataAtStart(entry.AsByteArray, source, time);
            }

            // The final complete data will be: [received drawing points] - [end line points for any unfinished line] - [already known drawing points] 
            InsertLocalDataAtStart(newData, source, time);
            RedrawFullDrawing();
        }
        #endregion

        public async void ClearPoints()
        {
            await Object.WaitForStateAuthority();
            NotifyTextureDrawerOfClearedPoints();
            ClearData();
        }

        protected override void OnDataCleared()
        {
            base.OnDataCleared();
            textureSurface.ResetTexture();
        }

        void NotifyTextureDrawerOfClearedPoints()
        {
            foreach (var drawer in FindObjectsOfType<TextureDrawer>())
            {
                drawer.ForgetTextureDrawingPoints(this);
            }
        }
    }
}
