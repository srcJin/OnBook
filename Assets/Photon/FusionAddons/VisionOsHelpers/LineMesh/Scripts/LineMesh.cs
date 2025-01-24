using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.VisionOsHelpers
{
    /**
    * 
    * The LineRenderer component is currently not compatible with Polyspatial. 
    * LineMesh offers a simple alternative, that creates a mesh.
    * The stored Points include a color property that will affect the mesh vertex colors.
    * A sample shader, LineSG, compatible with Polyspatial, uses those vertex color info to render the mesh.
    * The LineSGMaterial material in the addon is using this shader. 
    *  
    **/
    public class LineMesh : MonoBehaviour
    {
        [System.Serializable]
        public struct Point
        {
            public Vector3 relativePosition;
            public Color color;
            public float pressure;
        }
        public List<Point> points = new List<Point>();
        public float width = 0.01f;

        public Mesh mesh;

        [SerializeField]
        MeshFilter meshFilter;

        Vector3[] arbitraryDirections = new Vector3[] { Vector3.up, Vector3.left, Vector3.forward };
        public enum RefreshMode
        {
            OnDemand,
            OnPointCountChange,
        }

        public RefreshMode refreshMode = RefreshMode.OnPointCountChange;

        [Header("Debug")]
        public bool debugNormals = false;

        // Draw counter
        int addedPoints = 0;
        int lastRefreshPointCount = 0;

        // Actual mesh info
        public Vector3[] vertices = new Vector3[0];
        public Color32[] colors32 = new Color32[0];
        public Vector2[] uv = new Vector2[0];
        public int[] triangles = new int[0];

        const int VERTICES_PER_POINT = 4;
        const int TRIANGLE_PER_ADDITIONAL_POINT = 8;
        int VerticesCount
        {
            get
            {
                return VerticesCountForPointCount(points.Count);
            }
        }

        int TrianglesCount
        {
            get
            {
                return TriangleCountForPointCount(points.Count) * TRIANGLE_PER_ADDITIONAL_POINT;
            }
        }

        int VerticesCountForPointCount(int count)
        {
            return count * VERTICES_PER_POINT; ;
        }

        int TriangleCountForPointCount(int count)
        {
            if (count == 0) return 0;
            // No triangles for 1 point only
            return (count - 1) * TRIANGLE_PER_ADDITIONAL_POINT;
        }

        private void Awake()
        {
            InitMesh();
        }

        void InitMesh()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (mesh == null)
            {
                mesh = new Mesh();
                if (meshFilter) meshFilter.mesh = mesh;
            }
        }

        float WidthAtPoint(int i)
        {
            if (i >= points.Count) return 0;
            return width * points[i].pressure;
        }

        void ExtendMeshData()
        {
            ExtendData<Vector3>(ref vertices, VerticesCount);
            ExtendData<Color32>(ref colors32, VerticesCount);
            ExtendData(ref uv, VerticesCount);
            ExtendData<int>(ref triangles, TrianglesCount * 3);
        }

        static void ExtendData<T>(ref T[] data, int newDataLength)
        {
            if (data.Length == newDataLength) return;
            T[] newData = new T[newDataLength];
            Array.Copy(data, 0, newData, 0, Math.Min(data.Length, newDataLength));
            data = newData;
        }

        void RefreshMesh()
        {
            InitMesh();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors32 = colors32;
            mesh.triangles = triangles;
            mesh.uv = uv;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }

        [ContextMenu("Update mesh")]
        // Update the mesh incrementaly
        public void UpdateMesh()
        {
            ExtendMeshData();
            bool meshChange = false;
            while ((points.Count - 1) > addedPoints)
            {
                meshChange = true;
                var point = points[addedPoints];
                Point nextPoint = point;
                Vector3 planePosition = Vector3.zero;
                nextPoint = points[addedPoints + 1];
                if ((points.Count - 2) > addedPoints)
                {
                    planePosition = points[addedPoints + 2].relativePosition;
                }
                else
                {
                    // Not enough point: arbitrary reference
                    planePosition = nextPoint.relativePosition + Vector3.forward;
                }
                var planePositionValidityCheck = Vector3.Cross(nextPoint.relativePosition - point.relativePosition, planePosition - point.relativePosition);
                if (planePositionValidityCheck.sqrMagnitude == 0)
                {
                    foreach (var arbitraryDirection in arbitraryDirections)
                    {
                        planePosition = nextPoint.relativePosition + arbitraryDirection;
                        var validityCheck = Vector3.Cross(nextPoint.relativePosition - point.relativePosition, planePosition - point.relativePosition);
                        if (validityCheck.sqrMagnitude > 0)
                        {
                            // Valid
                            break;
                        }
                    }
                }
                AddPoint(point, nextPoint, planePosition, addedPoints);
                addedPoints++;
            }
            if (meshChange)
            {
                RefreshMesh();
            }
        }

        [ContextMenu("Reset mesh")]
        public void ResetMesh()
        {
            addedPoints = 0;
            vertices = new Vector3[0];
            colors32 = new Color32[0];
            uv = new Vector2[0];
            triangles = new int[0];
            RefreshMesh();
        }

        [ContextMenu("Rebuild mesh")]
        public void RebuildMesh()
        {
            ResetMesh();
            UpdateMesh();
        }

        // Add the vertices for the next point (and the first one if it is index 0), and the triangles to the next point
        void AddPoint(Point p0, Point p1, Vector3 planePosition, int index)
        {
            float w0 = WidthAtPoint(index);
            float w1 = WidthAtPoint(index + 1);
            Vector2 uv0 = new Vector2(0.5f, 0.5f);
            Vector2 uv1 = new Vector2(0.5f, 0.5f);
            Color32 color0 = p0.color;
            Color32 color1 = p1.color;
            Vector3 move = p1.relativePosition - p0.relativePosition;
            Vector3 normal = Vector3.Cross(move, planePosition - p0.relativePosition).normalized;
            Vector3 left = Vector3.Cross(move, normal).normalized;
            Vector3 l0 = p0.relativePosition + w0 * left;
            Vector3 r0 = p0.relativePosition - w0 * left;
            Vector3 u0 = p0.relativePosition + w0 * normal;
            Vector3 d0 = p0.relativePosition - w0 * normal;
            Vector3 l1 = p1.relativePosition + w1 * left;
            Vector3 r1 = p1.relativePosition - w1 * left;
            Vector3 u1 = p1.relativePosition + w1 * normal;
            Vector3 d1 = p1.relativePosition - w1 * normal;
            int baseIndex = VerticesCountForPointCount(index);
            int l0Index = baseIndex, u0Index = baseIndex + 1, r0Index = baseIndex + 2, d0Index = baseIndex + 3;
            int l1Index = baseIndex + 4, u1Index = baseIndex + 5, r1Index = baseIndex + 6, d1Index = baseIndex + 7;

            // Add vertices and color (uv should be added here too
            if (index == 0)
            {
                // The first point was never added before
                vertices[l0Index] = l0;
                vertices[u0Index] = u0;
                vertices[r0Index] = r0;
                vertices[d0Index] = d0;
                colors32[l0Index] = color0;
                colors32[u0Index] = color0;
                colors32[r0Index] = color0;
                colors32[d0Index] = color0;
                uv[l0Index] = uv0;
                uv[u0Index] = uv0;
                uv[r0Index] = uv0;
                uv[d0Index] = uv0;
            }
            vertices[l1Index] = l1;
            vertices[u1Index] = u1;
            vertices[r1Index] = r1;
            vertices[d1Index] = d1;
            colors32[l1Index] = color1;
            colors32[u1Index] = color1;
            colors32[r1Index] = color1;
            colors32[d1Index] = color1;
            uv[l1Index] = uv1;
            uv[u1Index] = uv1;
            uv[r1Index] = uv1;
            uv[d1Index] = uv1;

            // Add triangles to the next point
            int currentTriangleIndex = TriangleCountForPointCount(index + 1) * 3;
            AddTriangle(ref triangles, ref currentTriangleIndex, l0Index, l1Index, u0Index);
            AddTriangle(ref triangles, ref currentTriangleIndex, l1Index, u1Index, u0Index);
            AddTriangle(ref triangles, ref currentTriangleIndex, u0Index, u1Index, r1Index);
            AddTriangle(ref triangles, ref currentTriangleIndex, r1Index, r0Index, u0Index);
            AddTriangle(ref triangles, ref currentTriangleIndex, l0Index, l1Index, d0Index, reverse: true);
            AddTriangle(ref triangles, ref currentTriangleIndex, l1Index, d1Index, d0Index, reverse: true);
            AddTriangle(ref triangles, ref currentTriangleIndex, d0Index, d1Index, r1Index, reverse: true);
            AddTriangle(ref triangles, ref currentTriangleIndex, r1Index, r0Index, d0Index, reverse: true);
        }

        static void AddTriangle(ref int[] triangles, ref int currentIndex, int vertices0Index, int vertices1Index, int vertices2Index, bool reverse = false)
        {
            if (reverse == false)
            {
                triangles[currentIndex] = vertices0Index;
                triangles[currentIndex + 1] = vertices1Index;
                triangles[currentIndex + 2] = vertices2Index;
            }
            else
            {
                triangles[currentIndex + 2] = vertices0Index;
                triangles[currentIndex + 1] = vertices1Index;
                triangles[currentIndex] = vertices2Index;
            }
            currentIndex += 3;
        }

        private void Update()
        {
            if (refreshMode == RefreshMode.OnPointCountChange && lastRefreshPointCount != points.Count)
            {
                lastRefreshPointCount = points.Count;
                UpdateMesh();
            }
            if (debugNormals)
            {
                DebugNormals();
            }
        }

        void DebugNormals()
        {
            for (int i = 0; i < (points.Count - 1); i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];
                Vector3 planePosition;
                if ((points.Count - 2) > i)
                {
                    planePosition = points[i + 2].relativePosition;
                }
                else
                {
                    // Not enough point: arbitrary reference
                    planePosition = p1.relativePosition + Vector3.forward;
                }
                Vector3 move = p1.relativePosition - p0.relativePosition;
                Vector3 normal = Vector3.Cross(move, planePosition - p0.relativePosition).normalized;
                Vector3 left = Vector3.Cross(move, normal).normalized;
                Debug.DrawLine(transform.TransformPoint(p0.relativePosition), transform.TransformPoint(p0.relativePosition) + transform.TransformDirection(normal) * 0.1f, Color.green);
                Debug.DrawLine(transform.TransformPoint(p0.relativePosition), transform.TransformPoint(p0.relativePosition) + transform.TransformDirection(left) * 0.1f, Color.red);
                Debug.DrawLine(transform.TransformPoint(p0.relativePosition), transform.TransformPoint(p0.relativePosition) + transform.TransformDirection(move.normalized) * 0.1f, Color.blue);
            }
        }
    }
}
