using Fusion.Addons.DataSyncHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.LineDrawing
{
    /*
     * Representents a drawing, made of line
     * Only one user should be able to edit it, not made for multiple drawer edition. See TextureDrawing add-on for multi drawer use cases
     */
    [RequireComponent(typeof(LineDrawing))]
    public class NetworkLineDrawing : RingBufferLosslessSyncBehaviour<NetworkLineDrawing.LineDrawingPoint>
    {
        public interface INetworkLineDrawingListener
        {
            void DrawingFinished(NetworkLineDrawing drawing);
        }

        public GameObject finishedHandler;


        [Networked]
        [SerializeField] NetworkBool IsFinished { get; set; }

        [System.Serializable]
        [ByteArraySize(16)]
        public struct LineDrawingPoint : RingBuffer.IRingBufferEntry
        {
            public static float NEW_LINE_PRESSURE = -1;
            public float drawPressure;                      // A draw pressure of NEW_LINE_PRESSURE marks the start of a line
            public Vector3 localPosition;                   // for a draw pressure to NEW_LINE_PRESSURE, we use this field for the color

            public byte[] AsByteArray => SerializationTools.AsByteArray(localPosition, drawPressure);

            public void FillFromBytes(byte[] entryBytes)
            {
                int unserializePosition = 0;
                SerializationTools.Unserialize(entryBytes, ref unserializePosition, out localPosition);
                SerializationTools.Unserialize(entryBytes, ref unserializePosition, out drawPressure);
            }

            public static LineDrawingPoint NewLine(Color color) => new LineDrawingPoint { drawPressure = NEW_LINE_PRESSURE, localPosition = new Vector3(color.r, color.g, color.b) };

            public bool IsNewLine(out Color color)
            {
                color = default;
                if (drawPressure != NEW_LINE_PRESSURE) return false;
                color = new Color(r: localPosition.x, g: localPosition.y, b: localPosition.z);
                return true;
            }
        }

        Color currentLinecolor = Color.clear;
        public bool isDrawingLine = false;
        
        LineDrawing lineDrawing;
        
        public List<LineDrawingPoint> drawingPoints = new List<LineDrawingPoint>();

        int lastDrawnPoint = -1;


        [Header("Interpolation logic")]
        [SerializeField] bool drawPointsOnProxiesAsSoonAsAvailable = false;


        private void Awake()
        {
            lineDrawing = GetComponent<LineDrawing>();
            if(finishedHandler) finishedHandler.SetActive(false);
        }

        #region API for the state authority (drawing user)
        public void AddPoint(Vector3 worldPosition, float pressure, Color color)
        {
            if (color != currentLinecolor || isDrawingLine == false)
            {
                StartLine(color);
                currentLinecolor = color;
            }
            var localPosition = transform.InverseTransformPoint(worldPosition);
            var point = new LineDrawingPoint { drawPressure = pressure, localPosition = localPosition };
            AddEntry(point);
            drawingPoints.Add(point);
        }

        public void StartLine(Color color)
        {
            var point = LineDrawingPoint.NewLine(color);
            AddEntry(point);
            drawingPoints.Add(point);
            isDrawingLine = true;
        }

        public void StopLine()
        {
            isDrawingLine = false;
        }

        public void StopDrawing()
        {
            IsFinished = true;
        }

        void DrawAllPoints()
        {
            DrawPointsUpTo(drawingPoints.Count - 1);
        }
        #endregion

        #region Interfacing with LineDrawing
        // Require LineDrawing to create the index first points stored in drawingPoints
        void DrawPointsUpTo(int index)
        {
            if (index <= lastDrawnPoint) return;
            int i = lastDrawnPoint + 1;
            while (i <= index && i < drawingPoints.Count)
            {
                if (drawingPoints[i].IsNewLine(out var newLinecolor))
                {
                    lineDrawing.StartLine(newLinecolor);
                }
                else
                {
                    lineDrawing.AddPoint(drawingPoints[i].localPosition, drawingPoints[i].drawPressure);
                }
                lastDrawnPoint = i;
                i++;
            }
        }
        #endregion

        #region Data handling
        public override void OnNewEntries(byte[] newPaddingStartBytes, LineDrawingPoint[] newEntries)
        {
            if (lossRanges.Count != 0)
            {
                // Waiting for loss request: we skip those data to keep them orderered (we could store them to append them later)
                return;
            }

            foreach (var entry in newEntries)
            {
                drawingPoints.Add(entry);
            }

            if (IsFinished)
            {
                // New points to determine handler position
                RepositionHandler();
            }
        }

        protected override void OnNoLossRemaining()
        {
            LineDrawingPoint[] entriesArray = SplitCompleteData();

            drawingPoints = new List<LineDrawingPoint>(entriesArray);
        }
        #endregion

        public override void Render()
        {
            base.Render();
            if (lastDrawnPoint < (drawingPoints.Count - 1))
            {
                if (Object.HasStateAuthority || drawPointsOnProxiesAsSoonAsAvailable) {
                    DrawAllPoints();
                } 
                else
                {
                    bool proxyDrawingAuthorized = lossRanges.Count == 0;
                    // Find the from and to state of the drawing data (byte array),
                    //  find the entry count in each state (from drawing point count, to drawing point count),
                    //  and interpolate the current max point to draw with the interpolation alpha (between from and to point count)
                    if (proxyDrawingAuthorized && TryGetInterpolationTotalEntryCount(out int currentIndex))
                    {
                        DrawPointsUpTo(currentIndex);
                    }
                }
            }
            if(IsFinished && Object.HasStateAuthority && drawingPoints.Count == 1)
            {
                // No drawing
                Debug.Log("Empty: despawn");

                Runner.Despawn(Object);
            }
            if (finishedHandler && finishedHandler.activeSelf != IsFinished)
            {
                Debug.Log("Drawing finished "+ Object.Id);
                RepositionHandler();
            }
        }

        void RepositionHandler()
        {
            if (finishedHandler)
            {
                finishedHandler.SetActive(IsFinished);
                if (IsFinished && finishedHandler.gameObject.TryGetComponent<INetworkLineDrawingListener>(out var listener))
                {
                    listener.DrawingFinished(this);
                }
            }
        }
    }
}
