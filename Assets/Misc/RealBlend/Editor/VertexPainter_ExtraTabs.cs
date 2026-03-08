using UnityEngine;
using UnityEditor;

namespace RealBlend
{
    public interface IVertexPainterTab
    {
        void OnEnable();
        void OnDisable();
        void OnGUI();
        void OnSceneGUI(SceneView sceneView);
        void OnUndoRedo();
        void OnSelectionChange();
        void OnReload();
    }

    [System.Serializable]
    public class VertexPainter_CreationTab : IVertexPainterTab
    {
        private EditorWindow _owner;

        public enum PlaneOrientation { Floor_XZ, Wall_XY, Curved_Wall }
        public enum PivotMode { Mesh_Center, Circle_Center } // NEW Enum

        // --- Settings ---
        public PlaneOrientation orientation = PlaneOrientation.Floor_XZ;
        public PivotMode pivotMode = PivotMode.Mesh_Center; // Default to mesh center (easier usage)

        public float width = 5f;
        public float length = 5f;

        [Range(-360f, 360f)] public float curvature = 0f;
        [Range(0.01f, 1f)] public float completeness = 1f;

        public int resolutionPerMeter = 2;

        public bool showPreview = true;
        public bool showTriangles = false;
        public bool smartPreview = true;

        public Material defaultMaterial;

        public bool incrementMode = false;
        public float increment = 0.5f;

        // --- Internal State ---
        private Vector3 _previewPos;
        private Quaternion _previewRot;
        private bool _hasValidPreview = false;

        public VertexPainter_CreationTab(EditorWindow owner) { _owner = owner; }

        public void OnGUI()
        {
            GUILayout.Label("Create Paintable Mesh", EditorStyles.boldLabel);

            GUILayout.Space(10);
            orientation = (PlaneOrientation)EditorGUILayout.EnumPopup("Orientation", orientation);

            // NEW: Show Pivot Mode only when Curved
            if (orientation == PlaneOrientation.Curved_Wall)
            {
                pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Pivot Location", pivotMode);
                if (pivotMode == PivotMode.Circle_Center)
                    EditorGUILayout.HelpBox("Pivot is at the center of the 'Room'. Wall will spawn (Radius) units away.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("Pivot is on the Wall itself. Best for slight curves.", MessageType.None);
            }

            width = EditorGUILayout.FloatField("Width (Arc Length)", width);
            string lenLabel = orientation == PlaneOrientation.Floor_XZ ? "Length (Z)" : "Height (Y)";
            length = EditorGUILayout.FloatField(lenLabel, length);

            GUILayout.Space(5);

            if (orientation == PlaneOrientation.Curved_Wall)
            {
                EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
                curvature = EditorGUILayout.Slider("Curvature (Deg)", curvature, -360f, 360f);

                float compPercent = completeness * 100f;
                compPercent = EditorGUILayout.Slider("Completeness %", compPercent, 1f, 100f);
                completeness = compPercent / 100f;
            }
            else
            {
                curvature = 0f;
                completeness = 1f;
            }

            GUILayout.Space(10);
            resolutionPerMeter = EditorGUILayout.IntSlider("Density (Verts/m)", resolutionPerMeter, 1, 10);
            defaultMaterial = (Material)EditorGUILayout.ObjectField("Default Material", defaultMaterial, typeof(Material), false);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
            if (showPreview)
                showTriangles = EditorGUILayout.Toggle("Show Triangles", showTriangles);
            GUILayout.EndHorizontal();
            if (showPreview)
                smartPreview = EditorGUILayout.Toggle("Smart Preview", smartPreview);

            GUILayout.Space(5);
            incrementMode = EditorGUILayout.Toggle("Increment Mode", incrementMode);
            if (incrementMode)
            {
                increment = EditorGUILayout.FloatField("Increment Value", increment);
            }

            GUILayout.Space(15);

            // --- STATS ---
            int wCount, lCount;
            CalculateResolution(out wCount, out lCount);
            int vertCount = wCount * lCount;
            int triCount = (wCount - 1) * (lCount - 1) * 2;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Triangle Info:", EditorStyles.boldLabel);
            GUILayout.Label($" Vertices: {vertCount:N0}");
            GUILayout.Label($" Triangles: {triCount:N0}");
            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Mesh", GUILayout.Height(40)))
            {
                CreateMesh();
            }
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (!showPreview) return;

            UpdatePreviewPosition(sceneView);

            // Draw Origin Crosshair (Pivot Location)
            Handles.BeginGUI();
            Vector3 center = new Vector3(sceneView.position.width / 2, sceneView.position.height / 2, 0);
            Handles.color = new Color(0, 1, 1, 0.8f);
            Handles.DrawLine(center - new Vector3(15, 0, 0), center + new Vector3(15, 0, 0));
            Handles.DrawLine(center - new Vector3(0, 15, 0), center + new Vector3(0, 15, 0));
            Handles.EndGUI();

            if (!_hasValidPreview) return;

            Handles.matrix = Matrix4x4.TRS(_previewPos, _previewRot, Vector3.one);
            if (smartPreview)
            {
                Handles.color = new Color(1f, 0.92f, 0.016f, 0.45f);
                DrawPreviewOutline();

                Handles.color = new Color(1f, 0.92f, 0.016f, 0.8f);
                DrawDensitySample();
            }
            else
            {
                Handles.color = new Color(1f, 0.92f, 0.016f, 0.6f);
                DrawFullDensityPreview();
            }

            // VISUALIZE RADIUS (Helper Line)
            if (orientation == PlaneOrientation.Curved_Wall && Mathf.Abs(curvature) > 0.01f)
            {
                Handles.color = new Color(0, 1, 1, 0.3f);
                Vector3 meshCenter = GetVertexPosition(0.5f, 0.5f);

                // If Mesh Center mode: Pivot is at wall, Circle center is offset
                // If Circle Center mode: Pivot is at center, Wall is offset

                Vector3 circleCenter = Vector3.zero;
                if (pivotMode == PivotMode.Mesh_Center)
                {
                    // Calculate where the center WOULD be relative to the mesh
                    float radius = width / (curvature * Mathf.Deg2Rad);
                    circleCenter = new Vector3(0, 0, -radius);
                    Handles.DrawDottedLine(meshCenter, circleCenter, 5f);
                    Handles.Label(circleCenter, "Center of Curve");
                }
                else
                {
                    // Pivot IS the center
                    Handles.DrawDottedLine(Vector3.zero, meshCenter, 5f);
                    Handles.Label(Vector3.zero, "Pivot (Center of Curve)");
                }
            }

            Handles.matrix = Matrix4x4.identity;
            sceneView.Repaint();
        }

        private void DrawPreviewOutline()
        {
            bool isCurved = orientation == PlaneOrientation.Curved_Wall && Mathf.Abs(curvature) > 0.01f;
            if (isCurved)
            {
                float effectiveArcLength = Mathf.Max(0.01f, Mathf.Abs(width * completeness));
                int arcSegments = Mathf.Clamp(Mathf.CeilToInt(effectiveArcLength * 6f), 8, 128);

                DrawWidthEdge(0f, arcSegments);
                DrawWidthEdge(1f, arcSegments);
                Handles.DrawLine(GetVertexPosition(0f, 0f), GetVertexPosition(0f, 1f));
                Handles.DrawLine(GetVertexPosition(1f, 0f), GetVertexPosition(1f, 1f));
                return;
            }

            Vector3 p00 = GetVertexPosition(0f, 0f);
            Vector3 p10 = GetVertexPosition(1f, 0f);
            Vector3 p01 = GetVertexPosition(0f, 1f);
            Vector3 p11 = GetVertexPosition(1f, 1f);

            Handles.DrawLine(p00, p10);
            Handles.DrawLine(p10, p11);
            Handles.DrawLine(p11, p01);
            Handles.DrawLine(p01, p00);
        }

        private void DrawDensitySample()
        {
            float effectiveWidthMeters = Mathf.Max(0.01f, Mathf.Abs(width * completeness));
            float effectiveLengthMeters = Mathf.Max(0.01f, Mathf.Abs(length));

            float sampleWidthMeters = Mathf.Min(1f, effectiveWidthMeters);
            float sampleLengthMeters = Mathf.Min(1f, effectiveLengthMeters);

            float sampleXSpanNorm = sampleWidthMeters / effectiveWidthMeters;
            float sampleZSpanNorm = sampleLengthMeters / effectiveLengthMeters;
            float sampleXStartNorm = Mathf.Clamp01(0.5f - (sampleXSpanNorm * 0.5f));
            float sampleZStartNorm = 0f;

            int sampleXCells = Mathf.Max(1, Mathf.RoundToInt(sampleWidthMeters * resolutionPerMeter));
            int sampleZCells = Mathf.Max(1, Mathf.RoundToInt(sampleLengthMeters * resolutionPerMeter));

            Color baseColor = Handles.color;
            Color triangleColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.4f);

            for (int z = 0; z < sampleZCells; z++)
            {
                float zNorm = sampleZStartNorm + (z / (float)sampleZCells) * sampleZSpanNorm;
                float zNormNext = sampleZStartNorm + ((z + 1) / (float)sampleZCells) * sampleZSpanNorm;

                for (int x = 0; x < sampleXCells; x++)
                {
                    float xNorm = sampleXStartNorm + (x / (float)sampleXCells) * sampleXSpanNorm;
                    float xNormNext = sampleXStartNorm + ((x + 1) / (float)sampleXCells) * sampleXSpanNorm;

                    Vector3 p0 = GetVertexPosition(xNorm, zNorm);
                    Vector3 p1 = GetVertexPosition(xNormNext, zNorm);
                    Vector3 p2 = GetVertexPosition(xNorm, zNormNext);
                    Vector3 p3 = GetVertexPosition(xNormNext, zNormNext);

                    Handles.DrawLine(p0, p1);
                    Handles.DrawLine(p0, p2);
                    if (x == sampleXCells - 1) Handles.DrawLine(p1, p3);
                    if (z == sampleZCells - 1) Handles.DrawLine(p2, p3);

                    if (showTriangles)
                    {
                        Handles.color = triangleColor;
                        Handles.DrawLine(p0, p3);
                        Handles.color = baseColor;
                    }
                }
            }

            Vector3 labelPos = GetVertexPosition(sampleXStartNorm + sampleXSpanNorm, sampleZStartNorm + sampleZSpanNorm);
            Handles.Label(labelPos, "1m x 1m density sample");
        }

        private void DrawFullDensityPreview()
        {
            int wRes, lRes;
            CalculateResolution(out wRes, out lRes);

            // Safety cap
            if (wRes * lRes > 15000) return;

            Color baseColor = Handles.color;
            Color triangleColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.4f);

            for (int z = 0; z < lRes - 1; z++)
            {
                float zNorm = z / (float)(lRes - 1);
                float zNormNext = (z + 1) / (float)(lRes - 1);

                for (int x = 0; x < wRes - 1; x++)
                {
                    float xNorm = x / (float)(wRes - 1);
                    float xNormNext = (x + 1) / (float)(wRes - 1);

                    Vector3 p0 = GetVertexPosition(xNorm, zNorm);
                    Vector3 p1 = GetVertexPosition(xNormNext, zNorm);
                    Vector3 p2 = GetVertexPosition(xNorm, zNormNext);
                    Vector3 p3 = GetVertexPosition(xNormNext, zNormNext);

                    Handles.DrawLine(p0, p1);
                    Handles.DrawLine(p0, p2);

                    if (x == wRes - 2) Handles.DrawLine(p1, p3);
                    if (z == lRes - 2) Handles.DrawLine(p2, p3);

                    if (showTriangles)
                    {
                        Handles.color = triangleColor;
                        Handles.DrawLine(p0, p3);
                        Handles.color = baseColor;
                    }
                }
            }
        }

        private void DrawWidthEdge(float zNorm, int segments)
        {
            Vector3 previous = GetVertexPosition(0f, zNorm);
            for (int i = 1; i <= segments; i++)
            {
                float xNorm = i / (float)segments;
                Vector3 current = GetVertexPosition(xNorm, zNorm);
                Handles.DrawLine(previous, current);
                previous = current;
            }
        }

        private void CalculateResolution(out int wCount, out int lCount)
        {
            float effectiveWidth = width * completeness;
            int minW = 2;
            wCount = Mathf.Max(Mathf.RoundToInt(effectiveWidth * resolutionPerMeter) + 1, minW);
            lCount = Mathf.RoundToInt(length * resolutionPerMeter) + 1;
        }

        private Vector3 GetVertexPosition(float xNorm, float zNorm)
        {
            float xPos, yPos, zPos;

            if (orientation == PlaneOrientation.Curved_Wall && Mathf.Abs(curvature) > 0.01f)
            {
                float totalAngle = curvature * completeness;
                float totalRad = totalAngle * Mathf.Deg2Rad;
                float radius = width / (curvature * Mathf.Deg2Rad);

                float currentRad = (xNorm - 0.5f) * totalRad;

                float x = Mathf.Sin(currentRad) * radius;
                float z = Mathf.Cos(currentRad) * radius;

                // Pivot Logic
                if (pivotMode == PivotMode.Mesh_Center)
                {
                    z -= radius;
                }

                xPos = x;
                // FIX: Remove "- 0.5f". Now 0 is the floor, 1 is the top.
                yPos = zNorm * length;
                zPos = z;
            }
            else
            {
                float effectiveWidth = width * completeness;
                xPos = (xNorm - 0.5f) * effectiveWidth;

                if (orientation == PlaneOrientation.Wall_XY)
                {
                    // FIX: Pivot is now at the bottom feet of the wall
                    yPos = zNorm * length;
                    zPos = 0;
                }
                else // Floor_XZ
                {
                    yPos = 0;
                    zPos = (zNorm - 0.5f) * length;
                }
            }

            return new Vector3(xPos, yPos, zPos);
        }

        private void CreateMesh()
        {
            int wCount, lCount;
            CalculateResolution(out wCount, out lCount);

            Vector3[] vertices = new Vector3[wCount * lCount];
            Vector2[] uvs = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[(wCount - 1) * (lCount - 1) * 6];

            int vIndex = 0;
            int tIndex = 0;

            float effectiveArcLength = width * completeness;

            for (int z = 0; z < lCount; z++)
            {
                for (int x = 0; x < wCount; x++)
                {
                    float xNorm = x / (float)(wCount - 1);
                    float zNorm = z / (float)(lCount - 1);

                    vertices[vIndex] = GetVertexPosition(xNorm, zNorm);
                    uvs[vIndex] = new Vector2(xNorm * effectiveArcLength, zNorm * length);
                    colors[vIndex] = Color.clear;

                    if (x < wCount - 1 && z < lCount - 1)
                    {
                        triangles[tIndex] = vIndex;
                        triangles[tIndex + 1] = vIndex + wCount;
                        triangles[tIndex + 2] = vIndex + 1;
                        triangles[tIndex + 3] = vIndex + 1;
                        triangles[tIndex + 4] = vIndex + wCount;
                        triangles[tIndex + 5] = vIndex + wCount + 1;
                        tIndex += 6;
                    }
                    vIndex++;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = (Mathf.Abs(curvature) > 1f) ? "Curved Mesh" : "Flat Mesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            GameObject go = new GameObject(mesh.name);
            go.transform.position = _hasValidPreview ? _previewPos : Vector3.zero;
            go.transform.rotation = _hasValidPreview ? _previewRot : Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = defaultMaterial != null ? defaultMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            go.AddComponent<MeshCollider>();

            VertexPaintStorage storage = go.AddComponent<VertexPaintStorage>();
            storage.CaptureOriginals(mesh);
            storage.paintedColors = colors;

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Painted Mesh");
        }

        private void UpdatePreviewPosition(SceneView sceneView)
        {
            Camera cam = sceneView.camera;
            if (cam == null) return;

            // 1. Raycast
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hitFound = Physics.Raycast(ray, out RaycastHit hit);

            // Default to point in front of cam if no hit
            Vector3 targetPoint = hitFound ? hit.point : ray.GetPoint(5f);

            // 2. Calculate Rotation (Snap to 45 deg)
            float camY = cam.transform.eulerAngles.y;
            float snappedY = Mathf.Round(camY / 45f) * 45f;

            // FIX: Add 180 so the wall faces TOWARDS the camera
            Quaternion facingRot = Quaternion.Euler(0, snappedY, 0);

            // 3. Apply
            if (orientation == PlaneOrientation.Floor_XZ)
            {
                _previewPos = targetPoint + new Vector3(0, 0.02f, 0);
                _previewRot = facingRot;
            }
            else // Wall_XY or Curved
            {
                // FIX: No more vertical offset needed because GetVertexPosition puts pivot at bottom
                _previewPos = targetPoint;
                _previewRot = facingRot;
            }

            if (incrementMode && increment > 0f)
            {
                _previewPos.x = Mathf.Round(_previewPos.x / increment) * increment;
                _previewPos.y = Mathf.Round(_previewPos.y / increment) * increment;
                _previewPos.z = Mathf.Round(_previewPos.z / increment) * increment;
            }

            _hasValidPreview = true;
        }

        public void OnEnable() { }
        public void OnDisable() { }
        public void OnUndoRedo() { }
        public void OnSelectionChange() { }
        public void OnReload() { }
    }

}
