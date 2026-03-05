using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RealBlend
{
    [System.Serializable]
    public class VertexPainter_SculptTab : IVertexPainterTab
    {
        private EditorWindow _owner;

        // "Simplify" is now "Revert to Base" in the enum
        // Added advanced modes for direct surface editing.
        public enum SculptMode
        {
            Move = 0,
            Push = 1,
            Smooth = 2,
            Revert_Pose = 3,
            Refine_Add = 4,
            Remove = 5,
            Nudge = 6,
            Flatten_Height = 7,
            Select_Vertex = 8
        }
        public SculptMode currentMode = SculptMode.Move;

        // --- Brush Shape ---
        public enum BrushShape { Circle = 0, Square = 1 }
        public BrushShape brushShape = BrushShape.Circle;
        public enum LocalFlattenAxis { X = 0, Y = 2, Z = 4 }

        // For square brushes the user can choose separate width and height. Values represent world-space dimensions.
        public float squareBrushWidth = 1.0f;
        public float squareBrushHeight = 1.0f;

        // Cache the last hit position and normal from the scene view.
        private Vector3 lastHitPoint;
        private Vector3 lastHitNormal;
        private Vector3 lastHitRayDirection;
        private bool nudgeStrokeActive;
        private Vector3 nudgeLastHitPoint;
        private int selectedVertexIndex = -1;
        private int hoveredVertexIndex = -1;

        // --- Brush Settings ---
        public float brushSize = 1.0f;
        public float brushStrength = 0.5f;
        public Texture2D brushAlpha;
        public float brushRotation = 0f;
        public bool invertBrushAlpha = false;
        public bool autoSampleFlattenHeight = true;
        public LocalFlattenAxis flattenLocalAxis = LocalFlattenAxis.Y;
        public float flattenHeight = 0f;
        public float flattenHeightOffset = 0f;

        // --- Visuals ---
        public bool showVertexGizmo = true;
        public bool showTriangleGizmo = true;
        public Color gizmoColor = Color.yellow;
        public float gizmoSize = 0.03f;

        // --- State ---
        private GameObject currentSelection;
        private MeshFilter currentMeshFilter;
        private Mesh workingMesh;
        private VertexPaintStorage currentStorage;

        private Vector3[] currentVertices;
        private Vector3[] originalVertices;
        private bool meshIsDirty = true;

        private Dictionary<Vector3Int, List<int>> vertexBuckets;
        private Dictionary<Vector3Int, List<int>> triangleBuckets;
        private float bucketSize = 1.0f;

        private int cachedTriCount = 0;
        private int cachedVertCount = 0;

        public VertexPainter_SculptTab(EditorWindow owner)
        {
            _owner = owner;
        }

        public void OnEnable() { meshIsDirty = true; }
        public void OnDisable() { }

        public void OnSelectionChange()
        {
            if (Selection.activeGameObject != currentSelection)
            {
                currentSelection = null;
                selectedVertexIndex = -1;
                hoveredVertexIndex = -1;
                meshIsDirty = true;
            }
        }

        public void OnReload()
        {
            currentSelection = null;
            selectedVertexIndex = -1;
            hoveredVertexIndex = -1;
            workingMesh = null;
            meshIsDirty = true;
            ValidateMeshState();
            NormalizeFlattenAxisFromLegacyValue();
        }

        public void OnUndoRedo()
        {
            if (currentMeshFilter != null)
            {
                workingMesh = currentMeshFilter.sharedMesh;
                currentVertices = workingMesh.vertices;
                meshIsDirty = true;
                UpdateStats();
                _owner.Repaint();
            }
        }

        // --- GUI ---

        public void OnGUI()
        {
            ValidateMeshState();
            NormalizeFlattenAxisFromLegacyValue();

            GUILayout.Label("🗿 Mesh Sculpting", EditorStyles.boldLabel);

            string[] tabs = new string[]
            {
                "Pull (Move)",
                "Push",
                "Smooth",
                "Revert Pose",
                "Refine (Add Detail)",
                "Remove",
                "Nudge (Drag)",
                "Flatten Height",
                "Select Vertex"
            };
            float availableWidth = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 28f);
            int tabColumns = Mathf.Clamp(Mathf.FloorToInt(availableWidth / 145f), 1, 4);
            GUIStyle sculptModeStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fixedHeight = 34f
            };
            currentMode = (SculptMode)GUILayout.SelectionGrid((int)currentMode, tabs, tabColumns, sculptModeStyle);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);

            if (brushShape == BrushShape.Circle)
            {
                brushSize = EditorGUILayout.Slider("Size [ ]", brushSize, 0.1f, 10f);
            }

            if (currentMode == SculptMode.Refine_Add)
            {
                EditorGUILayout.HelpBox("Refine: Splits edges (1 -> 4). Creates cleaner 'square-like' topology than standard subdivision.", MessageType.Info);
                EditorGUILayout.HelpBox("Warning: Refine changes topology. In refined regions, Revert Pose cannot fully recover the original vertex layout/context.", MessageType.Warning);
            }
            else if (currentMode == SculptMode.Remove)
            {
                EditorGUILayout.HelpBox("Remove: Deletes front-facing triangles inside the brush area and keeps back-side faces on double-sided meshes.", MessageType.Warning);
            }
            else if (currentMode == SculptMode.Nudge)
            {
                EditorGUILayout.HelpBox("Nudge: Click and drag to slide surface points along the hit plane.", MessageType.Info);
                brushStrength = EditorGUILayout.Slider("Strength", brushStrength, 0.1f, 5.0f);
            }
            else if (currentMode == SculptMode.Flatten_Height)
            {
                EditorGUILayout.HelpBox("Flatten Height: Pulls vertices toward a selected local-space axis height.", MessageType.Info);
                brushStrength = EditorGUILayout.Slider("Strength", brushStrength, 0.1f, 5.0f);
                flattenLocalAxis = (LocalFlattenAxis)EditorGUILayout.EnumPopup("Local Axis", flattenLocalAxis);
                autoSampleFlattenHeight = EditorGUILayout.Toggle("Sample Height On Click", autoSampleFlattenHeight);
                flattenHeight = EditorGUILayout.FloatField("Target Height (Local Axis)", flattenHeight);
                flattenHeightOffset = EditorGUILayout.FloatField("Offset (+/-)", flattenHeightOffset);
                EditorGUILayout.LabelField("Effective Target", GetEffectiveFlattenHeight().ToString("0.###"));
            }
            else if (currentMode == SculptMode.Select_Vertex)
            {
                EditorGUILayout.HelpBox("Select Vertex: Hover shows the nearest vertex. Click to select it, then move it with the position handle.", MessageType.Info);
            }
            else
            {
                brushStrength = EditorGUILayout.Slider("Strength", brushStrength, 0.1f, 5.0f);
            }

            brushShape = (BrushShape)EditorGUILayout.EnumPopup("Brush Shape", brushShape);
            if (brushShape == BrushShape.Square)
            {
                squareBrushWidth = EditorGUILayout.FloatField("Width", Mathf.Max(0.1f, squareBrushWidth));
                squareBrushHeight = EditorGUILayout.FloatField("Height", Mathf.Max(0.1f, squareBrushHeight));
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Custom Brush", GUILayout.Width(100));
            brushAlpha = (Texture2D)EditorGUILayout.ObjectField(brushAlpha, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.EndHorizontal();

            if (brushAlpha != null)
            {
                brushRotation = EditorGUILayout.Slider("Rotation", brushRotation, 0f, 360f);
                invertBrushAlpha = EditorGUILayout.Toggle("Invert Alpha", invertBrushAlpha);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            showVertexGizmo = EditorGUILayout.Toggle("Show Vertices", showVertexGizmo);
            if (currentMode == SculptMode.Remove)
            {
                showTriangleGizmo = EditorGUILayout.Toggle("Show Triangles", showTriangleGizmo);
            }
            if (showVertexGizmo)
            {
                gizmoSize = EditorGUILayout.Slider("Dot Size", gizmoSize, 0.01f, 0.1f);
                gizmoColor = EditorGUILayout.ColorField("Dot Color", gizmoColor);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Topology Management", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Revert Topology & Keep Paint", "Restores original triangles but projects current colors onto them.")))
            {
                RevertTopologyKeepPaint();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recalculate Normals")) { if (workingMesh != null) workingMesh.RecalculateNormals(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (workingMesh != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("📊 Mesh Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Vertices:", $"{cachedVertCount}");
                EditorGUILayout.LabelField("Triangles:", $"{cachedTriCount}");
                EditorGUILayout.EndVertical();
            }

            if (Event.current.type == EventType.Repaint && currentMeshFilter != null && workingMesh != null)
            {
                BuildSpatialBuckets();
            }
        }

        void UpdateStats()
        {
            if (workingMesh != null)
            {
                cachedVertCount = workingMesh.vertexCount;
                cachedTriCount = workingMesh.triangles.Length / 3;
            }
        }

        // --- Scene Logic ---

        public void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            ValidateMeshState();
            NormalizeFlattenAxisFromLegacyValue();

            if (EditorWindow.focusedWindow != _owner && EditorWindow.focusedWindow != sceneView) return;
            if (currentSelection != Selection.activeGameObject) return;
            if (currentSelection == null || workingMesh == null) return;

            // --- OPTIMIZATION 1: Force Repaint on Mouse Move ---
            // This fixes the "Frozen Brush" issue where the gizmo lags behind the cursor
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                SceneView.RepaintAll();
            }

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.LeftBracket)
                {
                    if (brushShape == BrushShape.Circle) brushSize = Mathf.Max(0.1f, brushSize - 0.5f);
                    else { squareBrushWidth = Mathf.Max(0.1f, squareBrushWidth - 0.5f); squareBrushHeight = Mathf.Max(0.1f, squareBrushHeight - 0.5f); }
                    if (currentMeshFilter != null && workingMesh != null) BuildSpatialBuckets();
                    e.Use();
                }
                if (e.keyCode == KeyCode.RightBracket)
                {
                    if (brushShape == BrushShape.Circle) brushSize += 0.5f;
                    else { squareBrushWidth += 0.5f; squareBrushHeight += 0.5f; }
                    if (currentMeshFilter != null && workingMesh != null) BuildSpatialBuckets();
                    e.Use();
                }
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (currentMode == SculptMode.Select_Vertex)
            {
                DrawAndEditSelectedVertexHandle();
            }

            if (currentMeshFilter.gameObject.scene.GetPhysicsScene().Raycast(ray.origin, ray.direction, out RaycastHit hit))
            {
                if (hit.collider.gameObject != currentSelection) return;
                lastHitRayDirection = ray.direction;

                if (currentMode == SculptMode.Select_Vertex)
                {
                    hoveredVertexIndex = -1;
                    if (TrySelectVertexAtHit(hit, out int hoverCandidate))
                    {
                        hoveredVertexIndex = hoverCandidate;
                    }

                    if (e.type == EventType.Repaint && hoveredVertexIndex >= 0 && hoveredVertexIndex != selectedVertexIndex)
                    {
                        DrawVertexMarker(hoveredVertexIndex, new Color(1f, 0.95f, 0.25f, 1f), 0.07f);
                    }

                    if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && GUIUtility.hotControl == 0)
                    {
                        if (hoveredVertexIndex >= 0)
                        {
                            selectedVertexIndex = hoveredVertexIndex;
                            e.Use();
                        }
                    }

                    if (e.type == EventType.MouseUp && e.button == 0 && !e.alt)
                    {
                        nudgeStrokeActive = false;
                    }

                    return;
                }

                UnityEngine.Rendering.CompareFunction previousBrushZTest = Handles.zTest;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.color = new Color(1, 0.5f, 0, 0.5f);
                Vector3 brushDrawPoint = hit.point + hit.normal * 0.0025f;
                if (brushShape == BrushShape.Circle)
                {
                    Handles.DrawWireDisc(brushDrawPoint, hit.normal, brushSize);
                }
                else
                {
                    Matrix4x4 oldMatrix = Handles.matrix;
                    Quaternion rot = Quaternion.LookRotation(hit.normal);
                    Handles.matrix = Matrix4x4.TRS(brushDrawPoint, rot, Vector3.one);
                    Handles.DrawWireCube(Vector3.zero, new Vector3(squareBrushWidth, squareBrushHeight, 0));
                    Handles.matrix = oldMatrix;
                }
                Handles.zTest = previousBrushZTest;

                lastHitPoint = hit.point;
                lastHitNormal = hit.normal;

                // --- OPTIMIZATION 2: Only draw gizmos during Repaint ---
                if (e.type == EventType.Repaint)
                {
                    UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    if (showVertexGizmo) DrawVertexGizmos(hit.point);
                    if (currentMode == SculptMode.Remove && showTriangleGizmo) DrawTriangleGizmos(hit.point);
                    Handles.zTest = previousZTest;
                }

                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlID);

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && !e.alt)
                {
                    if (currentMode == SculptMode.Refine_Add)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            RefineMesh_Midpoint(hit.point, hit.normal);
                            e.Use();
                        }
                    }
                    else if (currentMode == SculptMode.Remove)
                    {
                        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                        {
                            RemoveTrianglesAtBrush(hit.point, hit.normal);
                            e.Use();
                        }
                    }
                    else if (currentMode == SculptMode.Nudge)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            nudgeStrokeActive = true;
                            nudgeLastHitPoint = hit.point;
                            e.Use();
                        }
                        else
                        {
                            if (!nudgeStrokeActive)
                            {
                                nudgeStrokeActive = true;
                                nudgeLastHitPoint = hit.point;
                            }

                            Vector3 nudgeDelta = hit.point - nudgeLastHitPoint;
                            nudgeLastHitPoint = hit.point;
                            if (nudgeDelta.sqrMagnitude > 0.0000001f)
                            {
                                SculptMesh(hit.point, hit.normal, false, nudgeDelta);
                            }
                            e.Use();
                        }
                    }
                    else
                    {
                        if (currentMode == SculptMode.Flatten_Height && e.type == EventType.MouseDown && autoSampleFlattenHeight)
                        {
                            Vector3 localHit = currentMeshFilter.transform.InverseTransformPoint(hit.point);
                            flattenHeight = GetFlattenAxisValue(localHit);
                        }

                        // --- OPTIMIZATION 3a: Dragging ---
                        // Pass 'false' to skip RecalculateNormals while dragging.
                        SculptMesh(hit.point, hit.normal, false, Vector3.zero);
                        e.Use();
                    }
                }

                // --- OPTIMIZATION 3b: Mouse Up ---
                // Only recalculate normals once the user releases the mouse.
                if (e.type == EventType.MouseUp && e.button == 0 && !e.alt)
                {
                    nudgeStrokeActive = false;
                    if (currentMode != SculptMode.Refine_Add && currentMode != SculptMode.Remove)
                    {
                        Undo.RecordObject(workingMesh, "Finalize Sculpt");
                        workingMesh.RecalculateNormals();
                        workingMesh.RecalculateBounds();
                    }
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                nudgeStrokeActive = false;
            }
            else if (currentMode == SculptMode.Select_Vertex)
            {
                hoveredVertexIndex = -1;
            }

            if (currentMode == SculptMode.Select_Vertex && e.type == EventType.MouseDown && e.button == 0 && !e.alt && GUIUtility.hotControl == 0)
            {
                selectedVertexIndex = -1;
                hoveredVertexIndex = -1;
                _owner.Repaint();
            }
        }

        // --- Core Logic ---

        void ValidateMeshState()
        {
            GameObject newSelection = Selection.activeGameObject;
            if (newSelection != currentSelection)
            {
                currentSelection = newSelection;
                meshIsDirty = true;
            }

            if (currentSelection == null) return;

            if (currentMeshFilter == null || currentMeshFilter.gameObject != currentSelection)
            {
                currentMeshFilter = currentSelection.GetComponent<MeshFilter>();
                currentStorage = currentSelection.GetComponent<VertexPaintStorage>();
            }

            if (currentMeshFilter != null)
            {
                workingMesh = currentMeshFilter.sharedMesh;
                if (meshIsDirty && workingMesh != null)
                {
                    currentVertices = workingMesh.vertices;
                    if (selectedVertexIndex >= currentVertices.Length) selectedVertexIndex = -1;
                    if (hoveredVertexIndex >= currentVertices.Length) hoveredVertexIndex = -1;
                    if (currentStorage != null && currentStorage.hasOriginalData && currentStorage.originalVertices != null)
                        originalVertices = currentStorage.originalVertices;
                    else
                        originalVertices = currentVertices;

                    BuildSpatialBuckets();
                    UpdateStats();
                    meshIsDirty = false;
                }
            }
        }

        void BuildSpatialBuckets()
        {
            if (currentVertices == null) return;
            vertexBuckets = new Dictionary<Vector3Int, List<int>>();
            triangleBuckets = new Dictionary<Vector3Int, List<int>>();
            float sizeForBucket = brushShape == BrushShape.Circle ? brushSize : Mathf.Max(squareBrushWidth, squareBrushHeight);
            bucketSize = Mathf.Clamp(sizeForBucket, 0.5f, 5.0f);

            Transform mt = currentMeshFilter.transform;

            // Build vertex buckets
            for (int i = 0; i < currentVertices.Length; i++)
            {
                Vector3 worldPos = mt.TransformPoint(currentVertices[i]);
                Vector3Int key = GetBucketID(worldPos);
                if (!vertexBuckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    vertexBuckets[key] = list;
                }
                list.Add(i);
            }

            // Build triangle buckets
            int[] tris = workingMesh.triangles;
            for (int ti = 0; ti < tris.Length / 3; ti++)
            {
                int i = ti * 3;
                Vector3 p1 = mt.TransformPoint(currentVertices[tris[i]]);
                Vector3 p2 = mt.TransformPoint(currentVertices[tris[i + 1]]);
                Vector3 p3 = mt.TransformPoint(currentVertices[tris[i + 2]]);
                Vector3 centroid = (p1 + p2 + p3) / 3f;
                Vector3Int key = GetBucketID(centroid);
                if (!triangleBuckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    triangleBuckets[key] = list;
                }
                list.Add(ti);
            }
        }

        Vector3Int GetBucketID(Vector3 pos)
        {
            return new Vector3Int(Mathf.FloorToInt(pos.x / bucketSize), Mathf.FloorToInt(pos.y / bucketSize), Mathf.FloorToInt(pos.z / bucketSize));
        }

        float GetBrushAlphaValue(Vector3 worldPos, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (brushAlpha == null) return 1.0f;
            Vector3 brushRight = Vector3.Cross(hitNormal, Vector3.up);
            if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(hitNormal, Vector3.right);
            brushRight.Normalize();
            Vector3 brushUp = Vector3.Cross(brushRight, hitNormal).normalized;

            Quaternion rot = Quaternion.AngleAxis(brushRotation, hitNormal);
            brushRight = rot * brushRight;
            brushUp = rot * brushUp;

            Vector3 delta = worldPos - hitPoint;
            float u = Vector3.Dot(delta, brushRight) / brushSize;
            float v = Vector3.Dot(delta, brushUp) / brushSize;
            float uvX = u * 0.5f + 0.5f;
            float uvY = v * 0.5f + 0.5f;

            if (uvX >= 0 && uvX <= 1 && uvY >= 0 && uvY <= 1)
            {
                float val = brushAlpha.GetPixelBilinear(uvX, uvY).r;
                return invertBrushAlpha ? 1.0f - val : val;
            }
            return 0f;
        }

        float GetBrushRangeWorld()
        {
            return brushShape == BrushShape.Circle ? brushSize : Mathf.Max(squareBrushWidth, squareBrushHeight) * 0.5f;
        }

        float GetSurfaceDepthToleranceWorld()
        {
            // Keep operations on the currently hovered side of the mesh.
            return Mathf.Max(0.0025f, GetBrushRangeWorld() * 0.1f);
        }

        float GetEffectiveFlattenHeight()
        {
            return flattenHeight + flattenHeightOffset;
        }

        void NormalizeFlattenAxisFromLegacyValue()
        {
            int axisValue = (int)flattenLocalAxis;
            switch (axisValue)
            {
                case 0:
                case 1:
                    flattenLocalAxis = LocalFlattenAxis.X;
                    break;
                case 2:
                case 3:
                    flattenLocalAxis = LocalFlattenAxis.Y;
                    break;
                case 4:
                case 5:
                    flattenLocalAxis = LocalFlattenAxis.Z;
                    break;
                default:
                    flattenLocalAxis = LocalFlattenAxis.Y;
                    break;
            }
        }

        float GetFlattenAxisValue(Vector3 localPos)
        {
            switch (flattenLocalAxis)
            {
                case LocalFlattenAxis.X: return localPos.x;
                case LocalFlattenAxis.Y: return localPos.y;
                case LocalFlattenAxis.Z: return localPos.z;
                default: return localPos.y;
            }
        }

        void SetFlattenAxisValue(ref Vector3 localPos, float value)
        {
            switch (flattenLocalAxis)
            {
                case LocalFlattenAxis.X:
                    localPos.x = value;
                    break;
                case LocalFlattenAxis.Y:
                    localPos.y = value;
                    break;
                case LocalFlattenAxis.Z:
                    localPos.z = value;
                    break;
                default:
                    localPos.y = value;
                    break;
            }
        }

        bool HasValidSelectedVertex()
        {
            return currentVertices != null && selectedVertexIndex >= 0 && selectedVertexIndex < currentVertices.Length;
        }

        void DrawVertexMarker(int vertexIndex, Color color, float sizeMultiplier)
        {
            if (currentVertices == null || vertexIndex < 0 || vertexIndex >= currentVertices.Length) return;
            Transform mt = currentMeshFilter.transform;
            Vector3 worldPos = mt.TransformPoint(currentVertices[vertexIndex]);

            Color oldColor = Handles.color;
            UnityEngine.Rendering.CompareFunction oldZ = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = color;
            float dotSize = HandleUtility.GetHandleSize(worldPos) * sizeMultiplier;
            Handles.SphereHandleCap(0, worldPos, Quaternion.identity, dotSize, EventType.Repaint);
            Handles.zTest = oldZ;
            Handles.color = oldColor;
        }

        bool TrySelectVertexAtHit(RaycastHit hit, out int vertexIndex)
        {
            vertexIndex = -1;
            if (workingMesh == null || currentVertices == null) return false;

            int[] tris = workingMesh.triangles;
            if (tris == null || tris.Length < 3) return false;
            Transform mt = currentMeshFilter.transform;

            if (hit.triangleIndex >= 0)
            {
                int triBase = hit.triangleIndex * 3;
                if (triBase + 2 < tris.Length)
                {
                    int a = tris[triBase];
                    int b = tris[triBase + 1];
                    int c = tris[triBase + 2];

                    int best = a;
                    float bestDist = (mt.TransformPoint(currentVertices[a]) - hit.point).sqrMagnitude;

                    float distB = (mt.TransformPoint(currentVertices[b]) - hit.point).sqrMagnitude;
                    if (distB < bestDist)
                    {
                        best = b;
                        bestDist = distB;
                    }

                    float distC = (mt.TransformPoint(currentVertices[c]) - hit.point).sqrMagnitude;
                    if (distC < bestDist)
                    {
                        best = c;
                    }

                    vertexIndex = best;
                    return true;
                }
            }

            // Fallback if triangleIndex isn't available.
            float closest = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < currentVertices.Length; i++)
            {
                float dist = (mt.TransformPoint(currentVertices[i]) - hit.point).sqrMagnitude;
                if (dist < closest)
                {
                    closest = dist;
                    closestIndex = i;
                }
            }

            if (closestIndex >= 0)
            {
                vertexIndex = closestIndex;
                return true;
            }

            return false;
        }

        void DrawAndEditSelectedVertexHandle()
        {
            if (!HasValidSelectedVertex()) return;

            Transform mt = currentMeshFilter.transform;
            Vector3 worldPos = mt.TransformPoint(currentVertices[selectedVertexIndex]);

            DrawVertexMarker(selectedVertexIndex, new Color(0.2f, 1f, 1f, 1f), 0.08f);

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(workingMesh, "Move Vertex");
                if (currentStorage != null) Undo.RecordObject(currentStorage, "Move Vertex Data");

                currentVertices[selectedVertexIndex] = mt.InverseTransformPoint(newWorldPos);
                workingMesh.vertices = currentVertices;
                workingMesh.RecalculateNormals();
                workingMesh.RecalculateBounds();

                if (currentStorage != null)
                {
                    currentStorage.currentVertices = workingMesh.vertices;
                }

                meshIsDirty = true;
                BuildSpatialBuckets();
                UpdateStats();
                _owner.Repaint();
                SceneView.RepaintAll();
            }
        }

        bool IsVertexFacingHit(int vertexIndex, Vector3 hitNormal)
        {
            if (workingMesh == null) return true;
            Vector3[] normals = workingMesh.normals;
            if (normals == null || normals.Length <= vertexIndex) return true;

            Transform mt = currentMeshFilter.transform;
            Vector3 worldNormal = mt.TransformDirection(normals[vertexIndex]).normalized;
            return Vector3.Dot(worldNormal, hitNormal) > 0.01f;
        }

        bool IsTriangleFacingHit(Vector3 wp1, Vector3 wp2, Vector3 wp3, Vector3 hitPoint, Vector3 hitNormal, float depthTolerance)
        {
            Vector3 triNormal = Vector3.Cross(wp2 - wp1, wp3 - wp1);
            if (triNormal.sqrMagnitude < 0.0000001f) return false;
            triNormal.Normalize();

            // Skip opposite side winding on double-sided surfaces.
            if (Vector3.Dot(triNormal, hitNormal) <= 0.01f) return false;

            // Skip triangles that are behind the hit plane.
            Vector3 centroid = (wp1 + wp2 + wp3) / 3f;
            float depth = Vector3.Dot(centroid - hitPoint, hitNormal);
            if (depth < -depthTolerance) return false;

            // Prefer faces with a similar camera-facing orientation to the hit.
            if (lastHitRayDirection.sqrMagnitude > 0.0000001f)
            {
                float faceToRay = Vector3.Dot(triNormal, -lastHitRayDirection.normalized);
                if (faceToRay <= 0.001f) return false;
            }

            return true;
        }

        // --- Features ---

        void DrawVertexGizmos(Vector3 worldCenter)
        {
            if (vertexBuckets == null) return;

            // --- OPTIMIZATION: Local Space Math ---
            // Instead of calling TransformPoint on every vertex in range (which is slow),
            // we transform the brush center to Local Space once and compare sqrMagnitude.
            Transform mt = currentMeshFilter.transform;
            Vector3 localCenter = mt.InverseTransformPoint(worldCenter);

            // Adjust local brush size based on object scale to ensure we catch all vertices
            float maxScale = Mathf.Max(mt.lossyScale.x, Mathf.Max(mt.lossyScale.y, mt.lossyScale.z));
            float localBrushSize = brushSize / maxScale;
            float localWidth = squareBrushWidth / maxScale;
            float localHeight = squareBrushHeight / maxScale;
            float sqrLocalSize = localBrushSize * localBrushSize;

            float sizeForRange = GetBrushRangeWorld();
            float depthTolerance = GetSurfaceDepthToleranceWorld();
            int range = Mathf.CeilToInt(sizeForRange / bucketSize) + 1;
            Vector3Int centerID = GetBucketID(worldCenter);

            Handles.color = gizmoColor;

            // Basis for Square Brush (Local Space)
            Vector3 localNormal = mt.InverseTransformDirection(lastHitNormal).normalized;
            Vector3 localRight = Vector3.Cross(localNormal, Vector3.up);
            if (localRight.sqrMagnitude < 0.001f) localRight = Vector3.Cross(localNormal, Vector3.right);
            localRight.Normalize();
            Vector3 localUp = Vector3.Cross(localRight, localNormal).normalized;

            float halfW = localWidth * 0.5f;
            float halfH = localHeight * 0.5f;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int id = centerID + new Vector3Int(x, y, z);
                        if (vertexBuckets.TryGetValue(id, out List<int> indices))
                        {
                            foreach (int i in indices)
                            {
                                Vector3 vPos = currentVertices[i]; // Raw local position
                                bool inside = false;

                                if (brushShape == BrushShape.Circle)
                                {
                                    // Fast distance check
                                    if ((vPos - localCenter).sqrMagnitude <= sqrLocalSize) inside = true;
                                }
                                else
                                {
                                    Vector3 delta = vPos - localCenter;
                                    float u = Vector3.Dot(delta, localRight);
                                    float v = Vector3.Dot(delta, localUp);
                                    inside = (Mathf.Abs(u) <= halfW && Mathf.Abs(v) <= halfH);
                                }

                                if (inside)
                                {
                                    // Only transform to world space if we actually need to draw it
                                    Vector3 worldPos = mt.TransformPoint(vPos);
                                    if (Vector3.Dot(worldPos - worldCenter, lastHitNormal) < -depthTolerance) continue;
                                    if (!IsVertexFacingHit(i, lastHitNormal)) continue;
                                    Handles.DotHandleCap(0, worldPos, Quaternion.identity, gizmoSize, EventType.Repaint);
                                }
                            }
                        }
                    }
                }
            }
        }

        void DrawTriangleGizmos(Vector3 worldCenter)
        {
            if (triangleBuckets == null) return;

            Transform mt = currentMeshFilter.transform;
            Vector3 localCenter = mt.InverseTransformPoint(worldCenter);

            float maxScale = Mathf.Max(mt.lossyScale.x, Mathf.Max(mt.lossyScale.y, mt.lossyScale.z));
            float localBrushSize = brushSize / maxScale;
            float localWidth = squareBrushWidth / maxScale;
            float localHeight = squareBrushHeight / maxScale;
            float sqrLocalSize = localBrushSize * localBrushSize;

            Vector3 localNormal = mt.InverseTransformDirection(lastHitNormal).normalized;
            Vector3 localRight = Vector3.Cross(localNormal, Vector3.up);
            if (localRight.sqrMagnitude < 0.001f) localRight = Vector3.Cross(localNormal, Vector3.right);
            localRight.Normalize();
            Vector3 localUp = Vector3.Cross(localRight, localNormal).normalized;

            float halfW = localWidth * 0.5f;
            float halfH = localHeight * 0.5f;

            float sizeForRange = GetBrushRangeWorld();
            float depthTolerance = GetSurfaceDepthToleranceWorld();
            int range = Mathf.CeilToInt(sizeForRange / bucketSize) + 1;
            Vector3Int centerID = GetBucketID(worldCenter);

            int[] tris = workingMesh.triangles;
            Vector3[] verts = currentVertices;

            Handles.color = gizmoColor;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int id = centerID + new Vector3Int(x, y, z);
                        if (triangleBuckets.TryGetValue(id, out List<int> indices))
                        {
                            foreach (int ti in indices)
                            {
                                int i = ti * 3;
                                Vector3 p1 = verts[tris[i]];
                                Vector3 p2 = verts[tris[i + 1]];
                                Vector3 p3 = verts[tris[i + 2]];

                                Vector3 centroid = (p1 + p2 + p3) / 3f;

                                bool inside = false;

                                if (brushShape == BrushShape.Circle)
                                {
                                    if ((centroid - localCenter).sqrMagnitude <= sqrLocalSize) inside = true;
                                }
                                else
                                {
                                    Vector3 delta = centroid - localCenter;
                                    float u = Vector3.Dot(delta, localRight);
                                    float v = Vector3.Dot(delta, localUp);
                                    inside = (Mathf.Abs(u) <= halfW && Mathf.Abs(v) <= halfH);
                                }

                                if (inside)
                                {
                                    Vector3 wp1 = mt.TransformPoint(p1);
                                    Vector3 wp2 = mt.TransformPoint(p2);
                                    Vector3 wp3 = mt.TransformPoint(p3);
                                    if (!IsTriangleFacingHit(wp1, wp2, wp3, worldCenter, lastHitNormal, depthTolerance)) continue;

                                    Handles.DrawLine(wp1, wp2);
                                    Handles.DrawLine(wp2, wp3);
                                    Handles.DrawLine(wp3, wp1);
                                }
                            }
                        }
                    }
                }
            }
        }

        // --- OPTIMIZATION: Added recalculateNormals bool ---
        void SculptMesh(Vector3 hitPoint, Vector3 hitNormal, bool recalculateNormals, Vector3 nudgeDeltaWorld)
        {
            if (vertexBuckets == null) return;

            // Note: For very high poly meshes, RecordObject can still be slow. 
            // If drag is still jittery, consider moving RecordObject to MouseDown.
            Undo.RecordObject(workingMesh, "Sculpt Mesh");

            float sizeForRange = GetBrushRangeWorld();
            int range = Mathf.CeilToInt(sizeForRange / bucketSize);
            Vector3Int centerID = GetBucketID(hitPoint);
            Transform mt = currentMeshFilter.transform;

            bool isSmoothing = currentMode == SculptMode.Smooth;
            bool isPushing = currentMode == SculptMode.Push;
            bool isReverting = currentMode == SculptMode.Revert_Pose;
            bool isPulling = currentMode == SculptMode.Move;
            bool isNudging = currentMode == SculptMode.Nudge;
            bool isFlattening = currentMode == SculptMode.Flatten_Height;

            Vector3 projectedNudge = Vector3.ProjectOnPlane(nudgeDeltaWorld, hitNormal);
            Vector3 nudgeDeltaLocal = mt.InverseTransformVector(projectedNudge);

            List<int> affectedIndices = new List<int>();
            for (int x = -range; x <= range; x++)
                for (int y = -range; y <= range; y++)
                    for (int z = -range; z <= range; z++)
                        if (vertexBuckets.TryGetValue(centerID + new Vector3Int(x, y, z), out List<int> list))
                            affectedIndices.AddRange(list);

            if (affectedIndices.Count == 0) return;

            Vector3 avgPos = Vector3.zero;
            if (isSmoothing)
            {
                foreach (int i in affectedIndices) avgPos += currentVertices[i];
                avgPos /= affectedIndices.Count;
            }

            Vector3 brushRight = Vector3.Cross(hitNormal, Vector3.up);
            if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(hitNormal, Vector3.right);
            brushRight.Normalize();
            Vector3 brushUp = Vector3.Cross(brushRight, hitNormal).normalized;

            float halfW = squareBrushWidth * 0.5f;
            float halfH = squareBrushHeight * 0.5f;
            bool verticesChanged = false;

            foreach (int i in affectedIndices)
            {
                Vector3 worldPos = mt.TransformPoint(currentVertices[i]);
                float falloff = 0f;
                float alphaMod = 1f;

                if (brushShape == BrushShape.Circle)
                {
                    float dist = Vector3.Distance(hitPoint, worldPos);
                    if (dist > brushSize) continue;
                    float ratio = dist / brushSize;
                    falloff = Mathf.Clamp01(1.0f - ratio);
                    falloff *= falloff;
                    alphaMod = GetBrushAlphaValue(worldPos, hitPoint, hitNormal);
                }
                else
                {
                    Vector3 delta = worldPos - hitPoint;
                    float u = Vector3.Dot(delta, brushRight);
                    float v = Vector3.Dot(delta, brushUp);
                    if (Mathf.Abs(u) > halfW || Mathf.Abs(v) > halfH) continue;
                    float normU = Mathf.Abs(u) / halfW;
                    float normV = Mathf.Abs(v) / halfH;
                    float ratio = Mathf.Max(normU, normV);
                    falloff = Mathf.Clamp01(1.0f - ratio);
                    falloff *= falloff;

                    if (brushAlpha != null)
                    {
                        float uNorm = u / (halfW * 2f) + 0.5f;
                        float vNorm = v / (halfH * 2f) + 0.5f;
                        if (uNorm >= 0f && uNorm <= 1f && vNorm >= 0f && vNorm <= 1f)
                        {
                            float val = brushAlpha.GetPixelBilinear(uNorm, vNorm).r;
                            alphaMod = invertBrushAlpha ? (1.0f - val) : val;
                        }
                        else
                        {
                            alphaMod = 0f;
                        }
                    }
                }

                falloff *= alphaMod;
                if (falloff <= 0.001f) continue;

                verticesChanged = true;

                if (isReverting)
                {
                    if (originalVertices != null && i < originalVertices.Length)
                        currentVertices[i] = Vector3.Lerp(currentVertices[i], originalVertices[i], brushStrength * 0.1f * falloff);
                }
                else if (isSmoothing)
                {
                    Vector3 localAvg = mt.InverseTransformPoint(avgPos);
                    currentVertices[i] = Vector3.Lerp(currentVertices[i], currentVertices[i] * 0.5f + localAvg * 0.5f, brushStrength * 0.0001f * falloff);
                }
                else if (isFlattening)
                {
                    Vector3 targetLocalPos = currentVertices[i];
                    SetFlattenAxisValue(ref targetLocalPos, GetEffectiveFlattenHeight());
                    currentVertices[i] = Vector3.Lerp(currentVertices[i], targetLocalPos, brushStrength * 0.1f * falloff);
                }
                else if (isNudging)
                {
                    currentVertices[i] += nudgeDeltaLocal * brushStrength * falloff;
                }
                else if (isPulling || isPushing)
                {
                    Vector3 moveDir = isPushing ? -hitNormal : hitNormal;
                    currentVertices[i] += mt.InverseTransformVector(moveDir) * brushStrength * 0.02f * falloff;
                }
            }

            if (verticesChanged)
            {
                workingMesh.vertices = currentVertices;

                // --- OPTIMIZATION Check ---
                if (recalculateNormals)
                {
                    workingMesh.RecalculateNormals();
                    workingMesh.RecalculateBounds();
                }
            }
        }

        // =================================================================================
        // NEW: REMOVE TRIANGLES
        // =================================================================================
        void RemoveTrianglesAtBrush(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (workingMesh == null) return;
            Undo.RecordObject(workingMesh, "Remove Triangles");
            if (currentStorage != null) Undo.RecordObject(currentStorage, "Remove Triangles Data");

            Transform mt = currentMeshFilter.transform;

            Vector3[] verts = workingMesh.vertices;
            Vector2[] uvs = workingMesh.uv;
            Color[] cols = workingMesh.colors;
            int[] tris = workingMesh.triangles;

            List<int> newTris = new List<int>();
            HashSet<int> usedVerts = new HashSet<int>();

            Vector3 brushRight = Vector3.Cross(hitNormal, Vector3.up);
            if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(hitNormal, Vector3.right);
            brushRight.Normalize();
            Vector3 brushUp = Vector3.Cross(brushRight, hitNormal).normalized;

            float halfW = squareBrushWidth * 0.5f;
            float halfH = squareBrushHeight * 0.5f;
            float depthTolerance = GetSurfaceDepthToleranceWorld();

            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];

                Vector3 wp1 = mt.TransformPoint(verts[a]);
                Vector3 wp2 = mt.TransformPoint(verts[b]);
                Vector3 wp3 = mt.TransformPoint(verts[c]);
                if (!IsTriangleFacingHit(wp1, wp2, wp3, hitPoint, hitNormal, depthTolerance))
                {
                    newTris.Add(a);
                    newTris.Add(b);
                    newTris.Add(c);
                    usedVerts.Add(a);
                    usedVerts.Add(b);
                    usedVerts.Add(c);
                    continue;
                }

                Vector3 centroid = (wp1 + wp2 + wp3) / 3f;

                bool inside = false;
                if (brushShape == BrushShape.Circle)
                {
                    inside = Vector3.Distance(hitPoint, centroid) <= brushSize;
                }
                else
                {
                    Vector3 delta = centroid - hitPoint;
                    float u = Vector3.Dot(delta, brushRight);
                    float v = Vector3.Dot(delta, brushUp);
                    inside = (Mathf.Abs(u) <= halfW && Mathf.Abs(v) <= halfH);
                }

                if (!inside)
                {
                    newTris.Add(a);
                    newTris.Add(b);
                    newTris.Add(c);
                    usedVerts.Add(a);
                    usedVerts.Add(b);
                    usedVerts.Add(c);
                }
            }

            if (newTris.Count == tris.Length) return;

            List<Vector3> newVerts = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<Color> newCols = new List<Color>();
            int[] indexMap = new int[verts.Length];
            int newIndex = 0;

            for (int i = 0; i < verts.Length; i++)
            {
                if (usedVerts.Contains(i))
                {
                    indexMap[i] = newIndex;
                    newVerts.Add(verts[i]);
                    if (uvs != null && uvs.Length > i) newUVs.Add(uvs[i]);
                    if (cols != null && cols.Length > i) newCols.Add(cols[i]);
                    newIndex++;
                }
                else
                {
                    indexMap[i] = -1;
                }
            }

            for (int i = 0; i < newTris.Count; i++)
            {
                newTris[i] = indexMap[newTris[i]];
            }

            workingMesh.Clear(false);
            workingMesh.vertices = newVerts.ToArray();
            if (uvs != null && uvs.Length > 0) workingMesh.uv = newUVs.ToArray();
            if (cols != null && cols.Length > 0) workingMesh.colors = newCols.ToArray();
            workingMesh.triangles = newTris.ToArray();
            workingMesh.RecalculateNormals();
            workingMesh.RecalculateTangents();
            workingMesh.RecalculateBounds();

            currentVertices = workingMesh.vertices;
            if (currentStorage != null)
            {
                currentStorage.paintedColors = workingMesh.colors;
                currentStorage.currentVertices = workingMesh.vertices;
                currentStorage.currentTriangles = workingMesh.triangles;
                currentStorage.currentUVs = workingMesh.uv;
            }
            meshIsDirty = true;
            BuildSpatialBuckets();
            UpdateStats();
            _owner.Repaint();
        }

        // =================================================================================
        // NEW: REFINE (1 -> 4 Subdivision)
        // =================================================================================

        struct EdgeKey
        {
            public int v1;
            public int v2;
            public EdgeKey(int a, int b)
            {
                v1 = Mathf.Min(a, b);
                v2 = Mathf.Max(a, b);
            }
            public override bool Equals(object obj) => obj is EdgeKey k && k.v1 == v1 && k.v2 == v2;
            public override int GetHashCode() => v1 * 397 ^ v2;
        }

        void RefineMesh_Midpoint(Vector3 hitPoint, Vector3 hitNormal)
        {
            Transform mt = currentMeshFilter.transform;
            int[] tris = workingMesh.triangles;
            Vector3[] verts = workingMesh.vertices;
            Vector2[] uvs = workingMesh.uv;
            Color[] cols = workingMesh.colors;

            HashSet<EdgeKey> edgesToSplit = new HashSet<EdgeKey>();

            Vector3 brushRight = Vector3.right;
            Vector3 brushUp = Vector3.up;
            float halfW = squareBrushWidth * 0.5f;
            float halfH = squareBrushHeight * 0.5f;
            if (brushShape == BrushShape.Square)
            {
                brushRight = Vector3.Cross(hitNormal, Vector3.up);
                if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(hitNormal, Vector3.right);
                brushRight.Normalize();
                brushUp = Vector3.Cross(brushRight, hitNormal).normalized;
            }

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i1 = tris[i]; int i2 = tris[i + 1]; int i3 = tris[i + 2];
                Vector3 p1 = mt.TransformPoint(verts[i1]);
                Vector3 p2 = mt.TransformPoint(verts[i2]);
                Vector3 p3 = mt.TransformPoint(verts[i3]);
                Vector3 center = (p1 + p2 + p3) / 3.0f;

                bool inside;
                if (brushShape == BrushShape.Circle)
                {
                    inside = Vector3.Distance(hitPoint, center) < brushSize;
                }
                else
                {
                    Vector3 delta = center - hitPoint;
                    float u = Vector3.Dot(delta, brushRight);
                    float v = Vector3.Dot(delta, brushUp);
                    inside = (Mathf.Abs(u) <= halfW && Mathf.Abs(v) <= halfH);
                }

                if (inside)
                {
                    edgesToSplit.Add(new EdgeKey(i1, i2));
                    edgesToSplit.Add(new EdgeKey(i2, i3));
                    edgesToSplit.Add(new EdgeKey(i3, i1));
                }
            }

            if (edgesToSplit.Count == 0) return;

            Undo.RecordObject(workingMesh, "Refine Mesh");
            if (currentStorage != null) Undo.RecordObject(currentStorage, "Refine Data");

            List<Vector3> newVerts = new List<Vector3>(verts);
            List<Vector2> newUVs = new List<Vector2>(uvs);
            List<Color> newColors = new List<Color>(cols);
            List<int> newTris = new List<int>();

            Dictionary<EdgeKey, int> edgeMidpoints = new Dictionary<EdgeKey, int>();

            int GetMidpoint(int a, int b)
            {
                EdgeKey key = new EdgeKey(a, b);
                if (edgeMidpoints.TryGetValue(key, out int idx)) return idx;

                Vector3 midPos = (verts[a] + verts[b]) * 0.5f;
                Vector2 midUV = (uvs.Length > 0) ? (uvs[a] + uvs[b]) * 0.5f : Vector2.zero;
                Color midCol = (cols.Length > 0) ? Color.Lerp(cols[a], cols[b], 0.5f) : Color.white;

                newVerts.Add(midPos);
                newUVs.Add(midUV);
                newColors.Add(midCol);

                int newIdx = newVerts.Count - 1;
                edgeMidpoints[key] = newIdx;
                return newIdx;
            }

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i1 = tris[i]; int i2 = tris[i + 1]; int i3 = tris[i + 2];
                EdgeKey e1 = new EdgeKey(i1, i2);
                EdgeKey e2 = new EdgeKey(i2, i3);
                EdgeKey e3 = new EdgeKey(i3, i1);

                bool split1 = edgesToSplit.Contains(e1);
                bool split2 = edgesToSplit.Contains(e2);
                bool split3 = edgesToSplit.Contains(e3);

                int splitCount = (split1 ? 1 : 0) + (split2 ? 1 : 0) + (split3 ? 1 : 0);

                if (splitCount == 0)
                {
                    newTris.Add(i1); newTris.Add(i2); newTris.Add(i3);
                }
                else if (splitCount == 3)
                {
                    int m1 = GetMidpoint(i1, i2);
                    int m2 = GetMidpoint(i2, i3);
                    int m3 = GetMidpoint(i3, i1);

                    newTris.Add(i1); newTris.Add(m1); newTris.Add(m3);
                    newTris.Add(m1); newTris.Add(i2); newTris.Add(m2);
                    newTris.Add(m3); newTris.Add(m2); newTris.Add(i3);
                    newTris.Add(m1); newTris.Add(m2); newTris.Add(m3);
                }
                else if (splitCount == 1)
                {
                    int a, b, c;
                    if (split1) { a = i1; b = i2; c = i3; }
                    else if (split2) { a = i2; b = i3; c = i1; }
                    else { a = i3; b = i1; c = i2; }

                    int m = GetMidpoint(a, b);
                    newTris.Add(c); newTris.Add(a); newTris.Add(m);
                    newTris.Add(c); newTris.Add(m); newTris.Add(b);
                }
                else if (splitCount == 2)
                {
                    int m1 = split1 ? GetMidpoint(i1, i2) : -1;
                    int m2 = split2 ? GetMidpoint(i2, i3) : -1;
                    int m3 = split3 ? GetMidpoint(i3, i1) : -1;

                    if (!split1)
                    {
                        newTris.Add(i3); newTris.Add(m3); newTris.Add(m2);
                        newTris.Add(i1); newTris.Add(i2); newTris.Add(m2);
                        newTris.Add(m2); newTris.Add(m3); newTris.Add(i1);
                    }
                    else if (!split2)
                    {
                        newTris.Add(i1); newTris.Add(m1); newTris.Add(m3);
                        newTris.Add(i2); newTris.Add(i3); newTris.Add(m3);
                        newTris.Add(m3); newTris.Add(m1); newTris.Add(i2);
                    }
                    else
                    {
                        newTris.Add(i2); newTris.Add(m2); newTris.Add(m1);
                        newTris.Add(i3); newTris.Add(i1); newTris.Add(m1);
                        newTris.Add(m1); newTris.Add(m2); newTris.Add(i3);
                    }
                }
            }

            workingMesh.Clear();
            workingMesh.vertices = newVerts.ToArray();
            workingMesh.uv = newUVs.ToArray();
            workingMesh.colors = newColors.ToArray();
            workingMesh.triangles = newTris.ToArray();

            workingMesh.RecalculateNormals();
            workingMesh.RecalculateTangents();

            currentVertices = workingMesh.vertices;
            if (currentStorage != null) currentStorage.paintedColors = workingMesh.colors;
            meshIsDirty = true;
            UpdateStats();
            _owner.Repaint();
        }

        // =================================================================================
        // NEW: REVERT TOPOLOGY + KEEP PAINT
        // =================================================================================

        void RevertTopologyKeepPaint()
        {
            if (currentStorage == null || !currentStorage.hasOriginalData || currentStorage.originalVertices == null)
            {
                EditorUtility.DisplayDialog("Error", "No original topology data found stored on this object.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Revert & Project?", "This will revert the mesh to its original shape/topology but attempt to keep your painted colors.\n\nVertex positions will be reset.", "Revert", "Cancel"))
            {
                Undo.RecordObject(workingMesh, "Revert Keep Paint");
                Undo.RecordObject(currentStorage, "Revert Keep Paint Data");

                Transform trans = currentMeshFilter.transform;

                Vector3[] highResVerts = workingMesh.vertices;
                Color[] highResColors = workingMesh.colors;

                Vector3[] targetVerts = currentStorage.originalVertices.ToArray();
                int[] targetTris = currentStorage.originalTriangles.ToArray();
                Vector2[] targetUVs = currentStorage.originalUVs.ToArray();

                Color[] newColors = new Color[targetVerts.Length];

                for (int i = 0; i < targetVerts.Length; i++)
                {
                    float closestDist = float.MaxValue;
                    int closestIndex = -1;

                    for (int j = 0; j < highResVerts.Length; j++)
                    {
                        float dist = (targetVerts[i] - highResVerts[j]).sqrMagnitude;
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestIndex = j;
                        }
                    }

                    if (closestIndex != -1 && highResColors != null && highResColors.Length > closestIndex)
                    {
                        newColors[i] = highResColors[closestIndex];
                    }
                    else
                    {
                        newColors[i] = Color.white;
                    }
                }

                workingMesh.Clear(false);
                workingMesh.vertices = targetVerts;
                workingMesh.triangles = targetTris;
                workingMesh.uv = targetUVs;
                workingMesh.colors = newColors;

                workingMesh.RecalculateNormals();
                workingMesh.RecalculateTangents();
                workingMesh.RecalculateBounds();

                currentStorage.paintedColors = newColors;
                currentStorage.currentVertices = targetVerts;
                currentStorage.currentTriangles = targetTris;
                currentStorage.currentUVs = targetUVs;

                currentVertices = targetVerts;
                meshIsDirty = true;
                UpdateStats();
                _owner.Repaint();
            }
        }
    }
}
