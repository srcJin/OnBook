using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.LineDrawing
{
    public class LineDrawing : MonoBehaviour
    {
        public GameObject linePrefab;
        public float maxWidth = 0.01f;
        public Material lineMaterial;

        LineRenderer currentLine;
        [SerializeField] List<Vector3> drawingPoints = new List<Vector3>();
        List<float> drawingPressures = new List<float>();
        List<float> drawingPathLength = new List<float>();
        Vector3 lastPoint;


        protected LineRenderer CreateLine(Color color)
        {
            LineRenderer line;
            if (linePrefab)
            {
                var lineGO = GameObject.Instantiate(linePrefab, transform);
                line = lineGO.GetComponent<LineRenderer>();
                if (line == null)
                {
                    line = lineGO.AddComponent<LineRenderer>();
                }
            }
            else
            {
                var lineGO = new GameObject("Line");
                lineGO.transform.parent = transform;
                line = lineGO.AddComponent<LineRenderer>();
            }
            line.useWorldSpace = false;
            line.startWidth = maxWidth;
            line.endWidth = maxWidth;
            line.widthMultiplier = maxWidth;
            if (lineMaterial != null) line.sharedMaterial = lineMaterial;
            line.transform.position = transform.position;
            line.transform.rotation = transform.rotation;
            line.startColor = color;
            line.endColor = color;
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        public void StartLine(Color color)
        {
            currentLine = CreateLine(color);
            drawingPoints.Clear();
            drawingPressures.Clear();
            drawingPathLength.Clear();
        }

        public void StopLine()
        {
            currentLine = null;
        }

        public void AddPoint(Vector3 localPosition, float pressure)
        {
            if (currentLine == null)
            {
                throw new System.Exception("No line started");
            }
            drawingPoints.Add(localPosition);
            drawingPressures.Add(pressure);
            currentLine.positionCount = drawingPoints.Count;
            currentLine.SetPositions(drawingPoints.ToArray());
            UpdateWidthCurve(localPosition);
        }

        void UpdateWidthCurve(Vector3 localPosition)
        {
            if (drawingPathLength.Count > 0)
            {
                var lastLength = drawingPathLength[drawingPathLength.Count - 1];
                var total = lastLength + Vector3.Distance(lastPoint, localPosition);
                drawingPathLength.Add(total);
                AnimationCurve widthCurve = new AnimationCurve();
                int index = 0;
                foreach (var length in drawingPathLength)
                {
                    if (index < drawingPressures.Count)
                    {
                        widthCurve.AddKey(length / total, drawingPressures[index]); ;
                    }
                    index++;

                }
                currentLine.widthCurve = widthCurve;
            }
            else
            {
                drawingPathLength.Add(0);
            }
            lastPoint = localPosition;
        }
    }
}
