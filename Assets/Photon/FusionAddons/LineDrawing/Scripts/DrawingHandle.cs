using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.LineDrawing
{
    public class DrawingHandle : MonoBehaviour, NetworkLineDrawing.INetworkLineDrawingListener
    {
        #region INetworkLineDrawingListener
        public void DrawingFinished(NetworkLineDrawing drawing) {
            if (drawing.drawingPoints.Count == 0)
            {
                return;
            }
            var pointSum = Vector3.zero;
            foreach (var point in drawing.drawingPoints) {
                var pos = drawing.transform.TransformPoint(point.localPosition);
                pointSum += pos;
            }
            var handlePos = pointSum / drawing.drawingPoints.Count;

            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward == Vector3.zero) forward = Vector3.forward;

            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            transform.position = handlePos;
        }
        #endregion
    }
}
