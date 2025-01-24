using Fusion;
#if XRSHARED_ADDON_AVAILABLE
using Fusion.XR.Shared;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.LineDrawing
{
    public class NetworkLineDrawer : NetworkBehaviour
    {
        public NetworkLineDrawing drawingPrefab;
        protected NetworkLineDrawing currentDrawing;
        public Transform tip;
        public float minimalDrawingDistance = 0.001f;
        Vector3 lastPosition;

        // There is a current drawing that is not finished
        public bool IsDrawing => currentDrawing != null;
        // The current drawing has a line that is not finished
        public bool IsDrawingLine => IsDrawing && currentDrawing.isDrawingLine == true;


        public Color color = Color.black;
#if XRSHARED_ADDON_AVAILABLE
        IColorProvider colorProvider;
#endif

        protected virtual void Awake()
        {
            if (tip == null) tip = transform;
#if XRSHARED_ADDON_AVAILABLE
            colorProvider = GetComponent<IColorProvider>();
#endif
        }

        public void StartDrawing()
        {
            if (currentDrawing)
            {
                currentDrawing.StopLine();
                currentDrawing.StopDrawing();
            }
            currentDrawing = Runner.Spawn(drawingPrefab, tip.position, tip.rotation);
        }

        public void StopDrawing()
        {
            if (currentDrawing)
            {
                currentDrawing.StopDrawing();
            }
            currentDrawing = null;
        }

        public void AddPoint(float pressure)
        {
            AddPoint(color, pressure);
        }

        public void AddPoint(Color color, float pressure)
        {
            // Security to avoid drawing a lot of point while staying in place
            if (currentDrawing && minimalDrawingDistance != 0)
            {
                var distance = Vector3.Distance(tip.position, lastPosition);
                if(distance < minimalDrawingDistance)
                {
                    return;
                }
            }

            if(currentDrawing == null)
            {
                StartDrawing();
            }
            lastPosition = tip.position;
            currentDrawing.AddPoint(tip.position, pressure, color);
        }

        public void StartLine(Color color)
        {
            if (currentDrawing == null)
            {
                StartDrawing();
            }
            currentDrawing.StartLine(color);
        }

        public void StopLine()
        {
            if (currentDrawing == null) return;
            currentDrawing.StopLine();
        }

        public override void Render()
        {
            base.Render();
#if XRSHARED_ADDON_AVAILABLE
            if (colorProvider != null)
            {
                color = colorProvider.CurrentColor;
            }
#endif
        }
    }
}
