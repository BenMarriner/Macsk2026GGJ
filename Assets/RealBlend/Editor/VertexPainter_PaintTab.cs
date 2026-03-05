using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RealBlend
{
    [System.Serializable]
    public class VertexPainter_PaintTab : IVertexPainterTab
    {
        private readonly EditorWindow _owner;
        private const string PaletteRelativeDirectory = "Assets/RealBlend/VertexColorPalettes";
        private const string PaletteLastUsedNameKey = "RealBlend_LastVertexColorPaletteName";
        private const string VertexColorPreviewShaderName = "RealBlend/Vertex Color Preview";
        private const string VertexColorPreviewShaderAssetPath = "Assets/RealBlend/Art/Shaders/VertexColorPreview.shader";

        public enum PaintLayer { Base_Rock = 0, Layer1 = 1, Layer2 = 2, Wetness = 3, Variation = 4 }
        public enum PaintWorkflow { LayeredBlend = 0, VertexColor = 1 }
        private enum VertexColorPreviewMode { RGB = 0, Alpha = 1 }
        private enum Channel { None, R, G, B, A }
        private enum VertexColorTarget { RGBA, RGB, R, G, B, A }

        // --- Brush Settings ---
        public float brushSize = 1.0f;
        public float brushStrength = 0.5f;
        public float brushHardness = .5f;
        public float mouseSensitivity = 1.0f; // Normalized 0..1 (maps to 0..0.5 drag strength multiplier)
        public Texture2D brushAlpha;
        public float brushRotation = 0f;
        public bool invertBrushAlpha = false;
        public bool showPreview = true;

        public PaintLayer activeLayer = PaintLayer.Layer1;
        public bool eraseMode = false;
        public bool lockSelection = false;
        [SerializeField] private PaintWorkflow paintWorkflow = PaintWorkflow.LayeredBlend;
        [SerializeField] private VertexColorTarget vertexColorTarget = VertexColorTarget.RGBA;
        [SerializeField] private Color vertexPaintColor = Color.white;
        [SerializeField] private Color vertexEraseColor = Color.clear;
        [SerializeField] private Color[] vertexColorPalette = new Color[10];
        [SerializeField] private bool[] vertexColorPaletteUsed = new bool[10];
        [SerializeField] private int selectedVertexPaletteSlot = 0;
        [SerializeField] private string paletteFileName = "DefaultPalette";
        [SerializeField] private string[] savedPaletteNames = new string[0];
        [SerializeField] private int selectedSavedPaletteIndex = -1;
        [SerializeField] private bool showVertexColorOverlay = false;
        [SerializeField] private float vertexColorOverlayOpacity = 0.85f;
        [SerializeField] private bool vertexColorOverlayXRay = false;
        [SerializeField] private VertexColorPreviewMode vertexColorPreviewMode = VertexColorPreviewMode.RGB;

        // --- Internal Data --
        private GameObject currentSelection;
        private MeshFilter currentMeshFilter;
        private MeshRenderer currentRenderer;
        private Mesh workingMesh;
        private VertexPaintStorage currentStorage;

        private Vector3[] originalVertices;
        private Vector3[] originalNormals;
        private Color[] storedColors;
        private Color[] previewColors;

        // Non-Serialized runtime cache (The source of the "disappearing" bug)
        private Dictionary<Vector3Int, List<int>> vertexBuckets;

        private float bucketSize = 1.0f;
        private bool meshIsDirty = true;
        private bool isBrushPreviewing = false;
        private bool isPaintModeEnabled = false;
        private Material vertexColorPreviewMaterial;

        // --- Tool State ---
        private Tool lastTool = Tool.None;

        [SerializeField] private Texture[] layerTextures = new Texture[5]; // CHANGED: Replaced individual textures with array
        private GUIStyle toggleStyle;

        private Vector3 lastHitPoint;
        private Vector3 lastHitNormal;
        private bool validHit = false;
        private Vector2 scrollPos;

        // --- Noise Module ---
        [SerializeField]
        private VertexPainterNoiseModule noiseModule = new VertexPainterNoiseModule();
        public VertexPaintStorage CurrentStorage => currentStorage;

        // --- NEW: Settings for layers ---
        [SerializeField] private bool showSettings = false;
        [SerializeField] private string[] layerLabels = { "Base", "Layer 1", "Layer 2", "Wetness", "Variation" };
        [SerializeField] private bool[] layerOverlay = { false, false, false, true, true };
        [SerializeField] private Channel[] layerChannels = { Channel.None, Channel.G, Channel.B, Channel.A, Channel.R };
        [SerializeField] private string[] texturePropNames = { "_Base_Albedo", "_Layer1_Albedo", "_Layer2_Albedo", "_Layer3_Albedo", "_Layer4_Albedo" };

        public VertexPainter_PaintTab(EditorWindow owner)
        {
            _owner = owner;
        }

        // --- Interface Implementation ---

        public void OnEnable()
        {
            CleanupAllPreview();
            meshIsDirty = true;
            ValidateMeshState();
            UpdateMaterialTextures();
            EnsureVertexPaletteState();
            RefreshSavedPaletteNames();
        }

        public void OnDisable()
        {
            CleanupAllPreview();
            CleanupVertexColorPreviewMaterial();
        }

        public void ForceStop()
        {
            SetPaintMode(false);
        }

        public void OnUndoRedo()
        {
            if (currentStorage != null)
            {
                currentStorage.ApplyColors();
                if (currentMeshFilter != null)
                {
                    workingMesh = currentMeshFilter.sharedMesh;
                    storedColors = workingMesh.colors;
                    meshIsDirty = true;
                }
                SceneView.RepaintAll();
            }
        }

        public void OnSelectionChange()
        {
            if (lockSelection && isPaintModeEnabled) return;

            if (Selection.activeGameObject != currentSelection)
            {
                CleanupAllPreview();
                ValidateMeshState();
                UpdateMaterialTextures();
            }
        }

        public void OnReload()
        {
            vertexBuckets = null;
            meshIsDirty = true;
            ValidateMeshState();
            UpdateMaterialTextures();
            EnsureVertexPaletteState();
            RefreshSavedPaletteNames();
            CleanupVertexColorPreviewMaterial();
        }

        void SetPaintMode(bool active)
        {
            if (isPaintModeEnabled == active) return;

            isPaintModeEnabled = active;

            if (isPaintModeEnabled)
            {
                lastTool = Tools.current;
                Tools.current = Tool.None;
            }
            else
            {
                Tools.current = lastTool;
                CleanupAllPreview();
            }

            SceneView.RepaintAll();
        }

        void CleanupAllPreview()
        {
            if (workingMesh && storedColors != null)
            {
                if (isBrushPreviewing)
                {
                    workingMesh.colors = storedColors;
                    workingMesh.UploadMeshData(false);
                }
            }

            isBrushPreviewing = false;
            if (workingMesh && storedColors != null)
                noiseModule?.ClearPreview(workingMesh, storedColors);
        }

        void UpdateMaterialTextures()
        {
            if (!currentRenderer)
            {
                return;
            }

            Material mat = currentRenderer.sharedMaterial;
            if (!mat)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                string prop = texturePropNames[i];
                if (!string.IsNullOrEmpty(prop) && mat.HasProperty(prop))
                {
                    layerTextures[i] = mat.GetTexture(prop);
                }
                else
                {
                    layerTextures[i] = null;
                }
            }

            // Enhanced logging
            string objName = currentSelection ? currentSelection.name : "No Selection";
            string matName = mat ? mat.name : "null";
        }

        void ValidateMeshState()
        {
            GameObject newSelection = Selection.activeGameObject;

            if (lockSelection && isPaintModeEnabled && currentSelection != null)
            {
                if (currentSelection == null)
                {
                    lockSelection = false;
                    CleanupAllPreview();
                }
                else if (newSelection != currentSelection)
                {
                    if (newSelection != null) Selection.activeGameObject = currentSelection;
                    return;
                }
            }

            bool dataLost = (vertexBuckets == null);
            bool selectionChanged = (newSelection != currentSelection);

            if (selectionChanged || dataLost)
            {
                if (selectionChanged)
                {
                    CleanupAllPreview();
                    currentSelection = newSelection;
                    workingMesh = null;
                    currentStorage = null;
                    currentMeshFilter = null;
                    currentRenderer = null;
                }

                meshIsDirty = true;
                vertexBuckets = null;
            }

            if (currentSelection == null) return;

            if (!currentMeshFilter || currentMeshFilter.gameObject != currentSelection)
            {
                currentMeshFilter = currentSelection.GetComponent<MeshFilter>();
                currentRenderer = currentSelection.GetComponent<MeshRenderer>();
            }

            UpdateMaterialTextures();

            if (!currentMeshFilter) return;

            if (!currentStorage)
                currentStorage = currentSelection.GetComponent<VertexPaintStorage>();

            Mesh actualMesh = currentMeshFilter.sharedMesh;
            if (!actualMesh) return;

            if (!workingMesh || !actualMesh.name.Contains("_Instance"))
            {
                if (!currentStorage)
                    return;
                if (!isPaintModeEnabled)
                    return;

                if (!actualMesh.name.Contains("_Instance"))
                {
                    Undo.RecordObject(currentMeshFilter, "Instantiate Mesh for Painting");
                    workingMesh = Object.Instantiate(actualMesh);
                    workingMesh.name = actualMesh.name + "_Instance";
                    currentMeshFilter.sharedMesh = workingMesh;
                }
                else
                {
                    workingMesh = actualMesh;
                }

                if (currentStorage && currentStorage.paintedColors != null && currentStorage.paintedColors.Length > 0)
                {
                    if (currentStorage.paintedColors.Length == workingMesh.vertexCount)
                        workingMesh.colors = currentStorage.paintedColors;
                }
                else
                {
                    Color[] currentCols = workingMesh.colors;
                    if (currentCols == null || currentCols.Length == 0 || IsAllWhite(currentCols))
                    {
                        Color[] cleanColors = new Color[workingMesh.vertexCount];
                        for (int i = 0; i < cleanColors.Length; i++) cleanColors[i] = new Color(0, 0, 0, 0);

                        workingMesh.colors = cleanColors;

                        if (currentStorage)
                        {
                            currentStorage.paintedColors = cleanColors;
                            EditorUtility.SetDirty(currentStorage);
                        }
                    }
                }

                storedColors = workingMesh.colors;
                meshIsDirty = true;
            }
        }

        bool IsAllWhite(Color[] cols)
        {
            if (cols == null || cols.Length == 0) return true;
            int checkCount = Mathf.Min(cols.Length, 10);
            for (int i = 0; i < checkCount; i++)
            {
                if (cols[i].r < 0.9f || cols[i].g < 0.9f || cols[i].b < 0.9f) return false;
            }
            return true;
        }

        public void OnGUI()
        {
            if (toggleStyle == null || toggleStyle.normal.background == null)
            {
                toggleStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    imagePosition = ImagePosition.ImageAbove,
                    fixedHeight = 96,
                    fixedWidth = 96,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }

            ValidateMeshState();

            try
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                Color defaultBg = GUI.backgroundColor;
                GUI.backgroundColor = isPaintModeEnabled ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button(isPaintModeEnabled ? "🎨 PAINTING ACTIVE" : "🔴 START PAINTING", GUILayout.Height(40)))
                {
                    SetPaintMode(!isPaintModeEnabled);
                    _owner.Repaint();
                }
                GUI.backgroundColor = defaultBg;

                if (currentSelection != null && currentSelection.GetComponent<Collider>() == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No Collider Detected! You cannot paint without a collider.", MessageType.Error);
                    if (GUILayout.Button("Fix: Add Mesh Collider"))
                    {
                        currentSelection.AddComponent<MeshCollider>();
                    }
                }

                GUILayout.Space(15);

                if (currentSelection != null && currentMeshFilter != null)
                {
                    if (currentStorage == null)
                    {
                        EditorGUILayout.HelpBox("Data not saved! Add Storage script.", MessageType.Warning);
                        if (GUILayout.Button("⚠️ Add Data Storage Script", GUILayout.Height(30)))
                        {
                            currentStorage = currentSelection.AddComponent<VertexPaintStorage>();
                            workingMesh = null;
                            _owner.Repaint();
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = Color.green;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("✅ Data Storage Active", EditorStyles.boldLabel);
                        GUILayout.Label($"Stored Vertices: {currentStorage.paintedColors?.Length ?? 0}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                        GUI.backgroundColor = defaultBg;
                    }
                }

                GUILayout.Space(10);
                paintWorkflow = (PaintWorkflow)EditorGUILayout.EnumPopup("Paint Workflow", paintWorkflow);
                if (paintWorkflow == PaintWorkflow.LayeredBlend)
                {
                    UpdateMaterialTextures();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                    GUILayout.Space(5);
                    GUILayout.Label("🎨 Active Layer Palette", EditorStyles.boldLabel);
                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    for (int i = 0; i < 3; i++)
                    {
                        PaintLayer pl = (PaintLayer)i;
                        GUI.backgroundColor = (activeLayer == pl) ? new Color(1f, 1f, 0.5f) : defaultBg;
                        GUIContent content = new GUIContent();
                        content.tooltip = GetTooltip(pl);
                        Texture tex = layerTextures[i];
                        string lbl = layerLabels[i];
                        if (tex != null)
                        {
                            content.text = "";
                            content.image = tex;
                        }
                        else
                        {
                            content.text = lbl.ToUpper();
                        }
                        if (GUILayout.Button(content, toggleStyle)) activeLayer = pl;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    for (int i = 3; i < 5; i++)
                    {
                        PaintLayer pl = (PaintLayer)i;
                        GUI.backgroundColor = (activeLayer == pl) ? new Color(1f, 1f, 0.5f) : defaultBg;
                        GUIContent content = new GUIContent();
                        content.tooltip = GetTooltip(pl);
                        Texture tex = layerTextures[i];
                        string lbl = layerLabels[i];
                        if (tex != null)
                        {
                            content.text = "";
                            content.image = tex;
                        }
                        else
                        {
                            content.text = lbl.ToUpper();
                        }
                        if (GUILayout.Button(content, toggleStyle)) activeLayer = pl;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);
                    }
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = defaultBg;
                }
                else
                {
                    EnsureVertexPaletteState();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Direct Vertex Color", EditorStyles.boldLabel);
                    vertexPaintColor = EditorGUILayout.ColorField("Paint Color", vertexPaintColor);
                    vertexEraseColor = EditorGUILayout.ColorField("Erase Color", vertexEraseColor);
                    vertexColorTarget = (VertexColorTarget)EditorGUILayout.EnumPopup("Channels", vertexColorTarget);
                    showVertexColorOverlay = EditorGUILayout.Toggle("Show Color Overlay", showVertexColorOverlay);
                    if (showVertexColorOverlay)
                    {
                        EditorGUI.indentLevel++;
                        vertexColorPreviewMode = (VertexColorPreviewMode)EditorGUILayout.EnumPopup("Overlay Mode", vertexColorPreviewMode);
                        vertexColorOverlayOpacity = EditorGUILayout.Slider("Overlay Opacity", vertexColorOverlayOpacity, 0.05f, 1.0f);
                        vertexColorOverlayXRay = EditorGUILayout.Toggle("X-Ray Overlay", vertexColorOverlayXRay);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Color", GUILayout.Width(90)))
                    {
                        SaveVertexPaintColorToPalette();
                    }
                    if (GUILayout.Button("Clear Selected", GUILayout.Width(110)))
                    {
                        ClearVertexPaletteSlot(selectedVertexPaletteSlot);
                    }
                    GUILayout.Label($"Selected Slot: {selectedVertexPaletteSlot + 1}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    DrawVertexPaletteSwatches();
                    GUILayout.Space(6);
                    DrawVertexPaletteFileControls();
                    EditorGUILayout.HelpBox("Click a swatch to load it as Paint Color. Add Color stores the current paint color into the first free slot (or selected slot when all slots are used). Use Shift to paint the erase color.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(15);
                GUILayout.Label("Brush Settings", EditorStyles.boldLabel);

                lockSelection = EditorGUILayout.Toggle("Lock Selection", lockSelection);
                brushSize = EditorGUILayout.Slider("Size", brushSize, 0.1f, 10f);
                brushStrength = EditorGUILayout.Slider("Opacity", brushStrength, 0.0f, 1f);
                brushHardness = EditorGUILayout.Slider("Hardness", brushHardness, 0.0f, 1.0f);
                mouseSensitivity = EditorGUILayout.Slider("Mouse Sensitivity", mouseSensitivity, 0.05f, 1.0f);

                GUILayout.Space(5);
                brushAlpha = (Texture2D)EditorGUILayout.ObjectField("Brush Alpha", brushAlpha, typeof(Texture2D), false);

                if (brushAlpha != null)
                {
                    EditorGUI.indentLevel++;
                    brushRotation = EditorGUILayout.Slider("Rotation", brushRotation, 0f, 360f);
                    invertBrushAlpha = EditorGUILayout.Toggle("Invert Texture", invertBrushAlpha);
                    EditorGUI.indentLevel--;
                }

                showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);

                GUILayout.Space(15);
                GUILayout.Label("Operations", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                string floodLabel = paintWorkflow == PaintWorkflow.LayeredBlend ? "Flood Fill Layer" : "Flood Paint Color";
                string clearLabel = paintWorkflow == PaintWorkflow.LayeredBlend ? "Clear Layer" : "Flood Erase Color";
                if (GUILayout.Button(floodLabel)) FloodFill(true);
                if (GUILayout.Button(clearLabel)) FloodFill(false);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (noiseModule != null && currentMeshFilter && workingMesh && storedColors != null)
                {
                    if (originalVertices == null || originalVertices.Length != workingMesh.vertexCount)
                    {
                        originalVertices = workingMesh.vertices;
                        originalNormals = workingMesh.normals;
                    }
                    noiseModule.DrawGUI(this, currentMeshFilter, workingMesh, ref storedColors, ref originalVertices);
                }

                if (paintWorkflow == PaintWorkflow.LayeredBlend)
                {
                    showSettings = EditorGUILayout.Foldout(showSettings, "Advanced Layer Settings");
                    if (showSettings)
                    {
                        EditorGUI.indentLevel++;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Layer", GUILayout.Width(80));
                        GUILayout.Label("Label", GUILayout.Width(70));
                        GUILayout.Label("Overlay", GUILayout.Width(50));
                        GUILayout.Label("Channel", GUILayout.Width(70));
                        GUILayout.Label("Tex Prop", GUILayout.Width(90));
                        GUILayout.EndHorizontal();

                        for (int i = 0; i < 5; i++)
                        {
                            PaintLayer pl = (PaintLayer)i;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(pl.ToString(), GUILayout.Width(80));
                            layerLabels[i] = EditorGUILayout.TextField(layerLabels[i], GUILayout.Width(70));
                            layerOverlay[i] = EditorGUILayout.Toggle(layerOverlay[i], GUILayout.Width(50));
                            layerChannels[i] = (Channel)EditorGUILayout.EnumPopup(layerChannels[i], GUILayout.Width(70));
                            texturePropNames[i] = EditorGUILayout.TextField(texturePropNames[i], GUILayout.Width(90));
                            GUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                GUILayout.FlexibleSpace();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void EnsureVertexPaletteState()
        {
            const int slotCount = 10;

            if (vertexColorPalette == null || vertexColorPalette.Length != slotCount)
            {
                Color[] resized = new Color[slotCount];
                if (vertexColorPalette != null)
                {
                    int copyCount = Mathf.Min(vertexColorPalette.Length, slotCount);
                    System.Array.Copy(vertexColorPalette, resized, copyCount);
                }
                vertexColorPalette = resized;
            }

            if (vertexColorPaletteUsed == null || vertexColorPaletteUsed.Length != slotCount)
            {
                bool[] resized = new bool[slotCount];
                if (vertexColorPaletteUsed != null)
                {
                    int copyCount = Mathf.Min(vertexColorPaletteUsed.Length, slotCount);
                    System.Array.Copy(vertexColorPaletteUsed, resized, copyCount);
                }
                vertexColorPaletteUsed = resized;
            }

            selectedVertexPaletteSlot = Mathf.Clamp(selectedVertexPaletteSlot, 0, slotCount - 1);
        }

        private int FindFirstEmptyVertexPaletteSlot()
        {
            EnsureVertexPaletteState();
            for (int i = 0; i < vertexColorPaletteUsed.Length; i++)
            {
                if (!vertexColorPaletteUsed[i]) return i;
            }
            return -1;
        }

        private void SaveVertexPaintColorToPalette()
        {
            EnsureVertexPaletteState();
            int slot = FindFirstEmptyVertexPaletteSlot();
            if (slot < 0) slot = selectedVertexPaletteSlot;

            vertexColorPalette[slot] = vertexPaintColor;
            vertexColorPaletteUsed[slot] = true;
            selectedVertexPaletteSlot = slot;
        }

        private void ClearVertexPaletteSlot(int slot)
        {
            EnsureVertexPaletteState();
            slot = Mathf.Clamp(slot, 0, vertexColorPaletteUsed.Length - 1);
            vertexColorPaletteUsed[slot] = false;
            vertexColorPalette[slot] = Color.clear;
        }

        private void DrawVertexPaletteSwatches()
        {
            EnsureVertexPaletteState();
            const int slotsPerRow = 5;
            Event e = Event.current;
            GUIStyle centeredLabel = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };

            EditorGUILayout.LabelField("Saved Colors", EditorStyles.miniBoldLabel);
            for (int row = 0; row < 2; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < slotsPerRow; col++)
                {
                    int slot = row * slotsPerRow + col;
                    bool used = vertexColorPaletteUsed[slot];
                    bool selected = selectedVertexPaletteSlot == slot;

                    Rect rect = GUILayoutUtility.GetRect(32f, 24f, GUILayout.Width(32), GUILayout.Height(24));
                    Color swatchColor = used ? vertexColorPalette[slot] : new Color(0.2f, 0.2f, 0.2f, 0.85f);
                    EditorGUI.DrawRect(rect, swatchColor);

                    if (!used)
                    {
                        Color oldContent = GUI.contentColor;
                        GUI.contentColor = selected ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
                        GUI.Label(rect, "+", centeredLabel);
                        GUI.contentColor = oldContent;
                    }

                    if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                    {
                        selectedVertexPaletteSlot = slot;
                        if (e.button == 1)
                        {
                            ClearVertexPaletteSlot(slot);
                        }
                        else if (e.button == 0 && used)
                        {
                            vertexPaintColor = vertexColorPalette[slot];
                        }
                        e.Use();
                    }

                    Color border = selected ? Color.white : new Color(0f, 0f, 0f, 0.5f);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
                    EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        [System.Serializable]
        private class VertexPaletteSaveData
        {
            public Color[] colors;
            public bool[] used;
            public int selectedSlot;
            public Color paintColor;
            public Color eraseColor;
            public int colorTarget;
        }

        private void DrawVertexPaletteFileControls()
        {
            EditorGUILayout.LabelField("Palette Files", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            paletteFileName = EditorGUILayout.TextField("Name", paletteFileName);
            if (GUILayout.Button("Save Palette", GUILayout.Width(100)))
            {
                SavePaletteToDisk();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (savedPaletteNames != null && savedPaletteNames.Length > 0)
            {
                selectedSavedPaletteIndex = EditorGUILayout.Popup("Saved", selectedSavedPaletteIndex, savedPaletteNames);
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    LoadSelectedPaletteFromDisk();
                }
            }
            else
            {
                GUILayout.Label("No saved palettes found", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshSavedPaletteNames();
            }
            EditorGUILayout.EndHorizontal();
        }

        private string GetPaletteDirectoryAbsolutePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), PaletteRelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        }

        private void EnsurePaletteDirectoryExists()
        {
            string directory = GetPaletteDirectoryAbsolutePath();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private string SanitizePaletteName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "DefaultPalette";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string result = rawName.Trim();
            foreach (char c in invalidChars)
            {
                result = result.Replace(c, '_');
            }
            return string.IsNullOrWhiteSpace(result) ? "DefaultPalette" : result;
        }

        private string GetPaletteFilePath(string paletteName)
        {
            return Path.Combine(GetPaletteDirectoryAbsolutePath(), $"{paletteName}.json");
        }

        private void RefreshSavedPaletteNames()
        {
            EnsurePaletteDirectoryExists();
            string directory = GetPaletteDirectoryAbsolutePath();
            string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            List<string> names = new List<string>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                names.Add(Path.GetFileNameWithoutExtension(files[i]));
            }
            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            savedPaletteNames = names.ToArray();

            if (savedPaletteNames.Length == 0)
            {
                selectedSavedPaletteIndex = -1;
                return;
            }

            string preferred = EditorPrefs.GetString(PaletteLastUsedNameKey, string.Empty);
            int preferredIndex = System.Array.IndexOf(savedPaletteNames, preferred);
            if (preferredIndex >= 0)
            {
                selectedSavedPaletteIndex = preferredIndex;
            }
            else if (selectedSavedPaletteIndex < 0 || selectedSavedPaletteIndex >= savedPaletteNames.Length)
            {
                selectedSavedPaletteIndex = 0;
            }
        }

        private void SavePaletteToDisk()
        {
            EnsureVertexPaletteState();
            EnsurePaletteDirectoryExists();

            string paletteName = SanitizePaletteName(paletteFileName);
            paletteFileName = paletteName;
            string filePath = GetPaletteFilePath(paletteName);

            VertexPaletteSaveData data = new VertexPaletteSaveData
            {
                colors = new Color[vertexColorPalette.Length],
                used = new bool[vertexColorPaletteUsed.Length],
                selectedSlot = selectedVertexPaletteSlot,
                paintColor = vertexPaintColor,
                eraseColor = vertexEraseColor,
                colorTarget = (int)vertexColorTarget
            };
            System.Array.Copy(vertexColorPalette, data.colors, vertexColorPalette.Length);
            System.Array.Copy(vertexColorPaletteUsed, data.used, vertexColorPaletteUsed.Length);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();

            RefreshSavedPaletteNames();
            int loadedIndex = System.Array.IndexOf(savedPaletteNames, paletteName);
            if (loadedIndex >= 0)
            {
                selectedSavedPaletteIndex = loadedIndex;
            }
            EditorPrefs.SetString(PaletteLastUsedNameKey, paletteName);
        }

        private void LoadSelectedPaletteFromDisk()
        {
            if (savedPaletteNames == null || savedPaletteNames.Length == 0) return;
            if (selectedSavedPaletteIndex < 0 || selectedSavedPaletteIndex >= savedPaletteNames.Length) return;

            string paletteName = savedPaletteNames[selectedSavedPaletteIndex];
            string filePath = GetPaletteFilePath(paletteName);
            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            VertexPaletteSaveData data = JsonUtility.FromJson<VertexPaletteSaveData>(json);
            if (data == null) return;

            EnsureVertexPaletteState();

            if (data.colors != null)
            {
                int count = Mathf.Min(data.colors.Length, vertexColorPalette.Length);
                for (int i = 0; i < count; i++) vertexColorPalette[i] = data.colors[i];
                for (int i = count; i < vertexColorPalette.Length; i++) vertexColorPalette[i] = Color.clear;
            }

            if (data.used != null)
            {
                int count = Mathf.Min(data.used.Length, vertexColorPaletteUsed.Length);
                for (int i = 0; i < count; i++) vertexColorPaletteUsed[i] = data.used[i];
                for (int i = count; i < vertexColorPaletteUsed.Length; i++) vertexColorPaletteUsed[i] = false;
            }

            selectedVertexPaletteSlot = Mathf.Clamp(data.selectedSlot, 0, vertexColorPalette.Length - 1);
            vertexPaintColor = data.paintColor;
            vertexEraseColor = data.eraseColor;
            if (System.Enum.IsDefined(typeof(VertexColorTarget), data.colorTarget))
            {
                vertexColorTarget = (VertexColorTarget)data.colorTarget;
            }
            else
            {
                vertexColorTarget = VertexColorTarget.RGBA;
            }

            paletteFileName = paletteName;
            EditorPrefs.SetString(PaletteLastUsedNameKey, paletteName);
            _owner.Repaint();
            SceneView.RepaintAll();
        }

        private bool EnsureVertexColorPreviewMaterial()
        {
            if (vertexColorPreviewMaterial != null) return true;

            Shader shader = Shader.Find(VertexColorPreviewShaderName);
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(VertexColorPreviewShaderAssetPath);
            }

            if (shader == null) return false;

            vertexColorPreviewMaterial = new Material(shader);
            vertexColorPreviewMaterial.name = "RealBlend_VertexColorPreview_Material";
            vertexColorPreviewMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return true;
        }

        private void CleanupVertexColorPreviewMaterial()
        {
            if (vertexColorPreviewMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(vertexColorPreviewMaterial);
                vertexColorPreviewMaterial = null;
            }
        }

        private void DrawVertexColorOverlay()
        {
            if (paintWorkflow != PaintWorkflow.VertexColor) return;
            if (!showVertexColorOverlay) return;
            if (!currentMeshFilter || !workingMesh) return;
            if (!EnsureVertexColorPreviewMaterial()) return;

            vertexColorPreviewMaterial.SetFloat("_Opacity", Mathf.Clamp01(vertexColorOverlayOpacity));
            vertexColorPreviewMaterial.SetFloat("_ShowAlpha", vertexColorPreviewMode == VertexColorPreviewMode.Alpha ? 1f : 0f);
            vertexColorPreviewMaterial.SetFloat("_ZTest", vertexColorOverlayXRay
                ? (float)UnityEngine.Rendering.CompareFunction.Always
                : (float)UnityEngine.Rendering.CompareFunction.LessEqual);

            if (vertexColorPreviewMaterial.SetPass(0))
            {
                Graphics.DrawMeshNow(workingMesh, currentMeshFilter.transform.localToWorldMatrix);
            }
        }

        private string GetTooltip(PaintLayer pl)
        {
            int idx = (int)pl;
            string mode = layerOverlay[idx] ? "Overlay" : "Blend";
            string ch = layerChannels[idx].ToString();
            if (ch == "None") ch = "Base (Remainder)";
            return $"{mode} Mode on {ch}";
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseUp && e.button == 0 && isPaintModeEnabled)
            {
                if (currentStorage != null && workingMesh != null)
                {
                    currentStorage.SaveCurrentState(workingMesh);
                }
            }

            if (!isPaintModeEnabled) return;

            ValidateMeshState();

            if (currentSelection == null || currentMeshFilter == null || workingMesh == null) return;
            if (currentMeshFilter.gameObject == null) return;

            if (meshIsDirty || vertexBuckets == null || storedColors == null)
            {
                BuildSpatialBuckets();
                meshIsDirty = false;
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.Repaint)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                PhysicsScene pScene = currentMeshFilter.gameObject.scene.GetPhysicsScene();

                if (pScene.IsValid() && pScene.Raycast(ray.origin, ray.direction, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == currentSelection)
                    {
                        lastHitPoint = hit.point;
                        lastHitNormal = hit.normal;
                        validHit = true;
                        if (e.type == EventType.MouseMove) sceneView.Repaint();
                    }
                    else validHit = false;
                }
                else validHit = false;
            }

            if (e.type == EventType.Repaint)
            {
                DrawVertexColorOverlay();
            }

            if (!validHit)
            {
                if (isBrushPreviewing) CleanupAllPreview();
                return;
            }

            float visualAlpha = Mathf.Lerp(0.2f, 1.0f, brushStrength);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            Color brushColor = (brushAlpha != null && invertBrushAlpha) ? new Color(1f, 0.4f, 0.2f, visualAlpha) : new Color(0, 1, 1, visualAlpha);
            Handles.color = brushColor;

            if (brushAlpha != null)
            {
                Vector3 brushRight = Vector3.Cross(lastHitNormal, Vector3.up);
                if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(lastHitNormal, Vector3.right);
                brushRight.Normalize();
                Vector3 brushUp = Vector3.Cross(brushRight, lastHitNormal).normalized;

                Quaternion rot = Quaternion.AngleAxis(brushRotation, lastHitNormal);
                brushRight = rot * brushRight;
                brushUp = rot * brushUp;

                Vector3 c1 = lastHitPoint - brushRight * brushSize - brushUp * brushSize;
                Vector3 c2 = lastHitPoint + brushRight * brushSize - brushUp * brushSize;
                Vector3 c3 = lastHitPoint + brushRight * brushSize + brushUp * brushSize;
                Vector3 c4 = lastHitPoint - brushRight * brushSize + brushUp * brushSize;

                Vector3 offset = lastHitNormal * 0.02f;
                c1 += offset; c2 += offset; c3 += offset; c4 += offset;

                Handles.DrawPolyLine(c1, c2, c3, c4, c1);

                if (brushHardness > 0.05f && brushHardness < 0.95f)
                {
                    Handles.color = new Color(brushColor.r, brushColor.g, brushColor.b, visualAlpha * 0.5f);
                    float innerSize = brushSize * brushHardness;

                    Vector3 i1 = lastHitPoint - brushRight * innerSize - brushUp * innerSize + offset;
                    Vector3 i2 = lastHitPoint + brushRight * innerSize - brushUp * innerSize + offset;
                    Vector3 i3 = lastHitPoint + brushRight * innerSize + brushUp * innerSize + offset;
                    Vector3 i4 = lastHitPoint - brushRight * innerSize + brushUp * innerSize + offset;

                    Handles.DrawPolyLine(i1, i2, i3, i4, i1);
                }
            }
            else
            {
                Handles.DrawWireDisc(lastHitPoint, lastHitNormal, brushSize);

                if (brushHardness > 0.05f && brushHardness < 0.95f)
                {
                    Handles.color = new Color(brushColor.r, brushColor.g, brushColor.b, visualAlpha * 0.5f);
                    Handles.DrawWireDisc(lastHitPoint, lastHitNormal, brushSize * brushHardness);
                }
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            bool isClick = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
            eraseMode = e.shift;

            if (isClick)
            {
                if (noiseModule != null) noiseModule.ClearPreview(workingMesh, storedColors);

                if (isBrushPreviewing)
                {
                    workingMesh.colors = storedColors;
                    workingMesh.UploadMeshData(false);
                    isBrushPreviewing = false;
                }

                if (e.type == EventType.MouseDown)
                {
                    Undo.RecordObject(workingMesh, "Vertex Paint");
                    if (currentStorage != null) Undo.RecordObject(currentStorage, "Vertex Paint Storage");
                }

                float strokeStrengthMultiplier = (e.type == EventType.MouseDrag) ? (Mathf.Clamp01(mouseSensitivity) * 0.5f) : 1.0f;
                ApplyBrushPaint(lastHitPoint, lastHitNormal, storedColors, storedColors, eraseMode, strokeStrengthMultiplier);
                workingMesh.colors = storedColors;
                workingMesh.UploadMeshData(false);

                if (currentStorage != null)
                {
                    currentStorage.paintedColors = storedColors;
                    EditorUtility.SetDirty(currentStorage);
                }

                e.Use();
            }
            else if (showPreview && (e.type == EventType.MouseMove || e.type == EventType.Repaint))
            {
                if (previewColors == null || previewColors.Length != storedColors.Length)
                    previewColors = new Color[storedColors.Length];

                System.Array.Copy(storedColors, previewColors, storedColors.Length);

                bool modified = ApplyBrushPaint(lastHitPoint, lastHitNormal, previewColors, previewColors, eraseMode);
                if (modified)
                {
                    workingMesh.colors = previewColors;
                    workingMesh.UploadMeshData(false);
                    isBrushPreviewing = true;
                }
                else if (isBrushPreviewing)
                {
                    CleanupAllPreview();
                }
            }
        }

        void BuildSpatialBuckets()
        {
            if (workingMesh == null) return;

            originalVertices = workingMesh.vertices;
            originalNormals = workingMesh.normals;
            storedColors = workingMesh.colors;

            if (storedColors == null || storedColors.Length != originalVertices.Length)
            {
                Color[] newColors = new Color[originalVertices.Length];
                for (int i = 0; i < newColors.Length; i++) newColors[i] = new Color(0, 0, 0, 0);
                storedColors = newColors;
                workingMesh.colors = storedColors;
            }

            if (previewColors == null || previewColors.Length != storedColors.Length)
                previewColors = new Color[storedColors.Length];

            System.Array.Copy(storedColors, previewColors, storedColors.Length);

            vertexBuckets = new Dictionary<Vector3Int, List<int>>();
            bucketSize = Mathf.Clamp(brushSize, 0.5f, 5.0f);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                if (!currentMeshFilter) break;

                Vector3 worldPos = currentMeshFilter.transform.TransformPoint(originalVertices[i]);
                Vector3Int key = GetBucketID(worldPos);
                if (!vertexBuckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    vertexBuckets[key] = list;
                }
                list.Add(i);
            }
        }

        Vector3Int GetBucketID(Vector3 pos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(pos.x / bucketSize),
                Mathf.FloorToInt(pos.y / bucketSize),
                Mathf.FloorToInt(pos.z / bucketSize)
            );
        }

        bool ApplyBrushPaint(Vector3 hitPoint, Vector3 hitNormal, Color[] sourceColors, Color[] destColors, bool isErasing, float strengthMultiplier = 1.0f)
        {
            if (vertexBuckets == null) return false;
            strengthMultiplier = Mathf.Max(0f, strengthMultiplier);

            int range = Mathf.CeilToInt(brushSize / bucketSize);
            Vector3Int centerID = GetBucketID(hitPoint);
            bool modified = false;

            Vector3 brushRight = Vector3.Cross(hitNormal, Vector3.up);
            if (brushRight.sqrMagnitude < 0.001f) brushRight = Vector3.Cross(hitNormal, Vector3.right);
            brushRight.Normalize();
            Vector3 brushUp = Vector3.Cross(brushRight, hitNormal).normalized;
            Quaternion rot = Quaternion.AngleAxis(brushRotation, hitNormal);
            brushRight = rot * brushRight;
            brushUp = rot * brushUp;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int id = centerID + new Vector3Int(x, y, z);
                        if (!vertexBuckets.TryGetValue(id, out List<int> indices)) continue;

                        foreach (int i in indices)
                        {
                            if (!currentMeshFilter) continue;

                            Vector3 vertWorldPos = currentMeshFilter.transform.TransformPoint(originalVertices[i]);
                            float dist = Vector3.Distance(hitPoint, vertWorldPos);

                            if (dist >= brushSize) continue;

                            float t = dist / brushSize;
                            float falloff;

                            if (brushHardness > 0.99f) falloff = 1.0f;
                            else
                            {
                                float fade = (t - brushHardness) / (1.0f - brushHardness);
                                falloff = 1.0f - Mathf.Clamp01(fade);
                                falloff = Mathf.SmoothStep(0, 1, falloff);
                            }

                            float alphaMod = 1.0f;
                            if (brushAlpha != null)
                            {
                                Vector3 delta = vertWorldPos - hitPoint;
                                float u = Vector3.Dot(delta, brushRight) / brushSize;
                                float v = Vector3.Dot(delta, brushUp) / brushSize;
                                float uvX = u * 0.5f + 0.5f;
                                float uvY = v * 0.5f + 0.5f;

                                if (uvX >= 0 && uvX <= 1 && uvY >= 0 && uvY <= 1)
                                {
                                    float sample = brushAlpha.GetPixelBilinear(uvX, uvY).r;
                                    alphaMod = invertBrushAlpha ? 1.0f - sample : sample;
                                }
                                else
                                {
                                    alphaMod = 0;
                                }
                            }

                            float finalStrength = falloff * brushStrength * alphaMod * strengthMultiplier;
                            if (finalStrength <= 0.001f) continue;

                            if (paintWorkflow == PaintWorkflow.VertexColor)
                            {
                                ApplyVertexColorBlend(i, finalStrength, sourceColors, destColors, isErasing, ref modified);
                            }
                            else
                            {
                                ApplyLayerBlend(i, finalStrength, sourceColors, destColors, isErasing, ref modified);
                            }
                        }
                    }
                }
            }

            return modified;
        }

        private void ApplyVertexColorBlend(int index, float strength, Color[] sourceColors, Color[] destColors, bool isErasing, ref bool modified)
        {
            Color source = sourceColors[index];
            Color target = isErasing ? vertexEraseColor : vertexPaintColor;
            Color result = source;

            float t = Mathf.Clamp01(strength);
            switch (vertexColorTarget)
            {
                case VertexColorTarget.R:
                    result.r = Mathf.Lerp(source.r, target.r, t);
                    break;
                case VertexColorTarget.G:
                    result.g = Mathf.Lerp(source.g, target.g, t);
                    break;
                case VertexColorTarget.B:
                    result.b = Mathf.Lerp(source.b, target.b, t);
                    break;
                case VertexColorTarget.A:
                    result.a = Mathf.Lerp(source.a, target.a, t);
                    break;
                case VertexColorTarget.RGB:
                    result.r = Mathf.Lerp(source.r, target.r, t);
                    result.g = Mathf.Lerp(source.g, target.g, t);
                    result.b = Mathf.Lerp(source.b, target.b, t);
                    break;
                default:
                    result = Color.Lerp(source, target, t);
                    break;
            }

            if (Mathf.Abs(result.r - source.r) > 0.0001f ||
                Mathf.Abs(result.g - source.g) > 0.0001f ||
                Mathf.Abs(result.b - source.b) > 0.0001f ||
                Mathf.Abs(result.a - source.a) > 0.0001f)
            {
                modified = true;
            }

            destColors[index] = result;
        }

        public void ApplyLayerBlend(int index, float strength, Color[] sourceColors, Color[] destColors, bool isErasing, ref bool modified)
        {
            Color c = sourceColors[index];

            int layerIdx = (int)activeLayer;
            bool isOverlayMode = layerOverlay[layerIdx];
            Channel layerChannel = layerChannels[layerIdx];

            // Collect weight channels: all channels used by non-overlay layers with channel != None
            List<Channel> weightChannels = new List<Channel>();
            for (int i = 0; i < 5; i++)
            {
                if (!layerOverlay[i] && layerChannels[i] != Channel.None)
                {
                    if (!weightChannels.Contains(layerChannels[i]))
                    {
                        weightChannels.Add(layerChannels[i]);
                    }
                }
            }

            // Get weights
            Dictionary<Channel, float> wDict = new Dictionary<Channel, float>();
            float wSum = 0f;
            foreach (Channel wc in weightChannels)
            {
                float val = GetChannel(c, wc);
                wDict[wc] = val;
                wSum += val;
            }
            float wBase = Mathf.Clamp01(1f - wSum);

            strength = Mathf.Clamp01(strength);

            if (isOverlayMode)
            {
                if (layerChannel == Channel.None) return; // Cannot overlay on none

                float currentVal = GetChannel(c, layerChannel);
                float target = isErasing ? 0f : 1f;
                currentVal = Mathf.Lerp(currentVal, target, strength);
                SetChannel(ref c, layerChannel, currentVal);
                modified = true;
            }
            else // Blend mode
            {
                if (layerChannel == Channel.None) // Base layer
                {
                    if (!isErasing)
                    {
                        float available = wSum;
                        if (available > 0f)
                        {
                            float amount = strength * available;
                            foreach (Channel wc in weightChannels)
                            {
                                float prop = wDict[wc] / available;
                                float newVal = wDict[wc] - amount * prop;
                                SetChannel(ref c, wc, newVal);
                                modified = true;
                            }
                        }
                    }
                    // Erasing base does nothing
                }
                else // Layer with channel
                {
                    if (weightChannels.Contains(layerChannel))
                    {
                        float wThis = wDict[layerChannel];
                        float pool = wBase;
                        foreach (Channel wc in weightChannels)
                        {
                            if (wc != layerChannel) pool += wDict[wc];
                        }

                        if (!isErasing)
                        {
                            if (pool > 0f)
                            {
                                float amount = strength * pool;
                                float newWThis = wThis + amount;
                                SetChannel(ref c, layerChannel, newWThis);

                                float propBase = wBase / pool;
                                // wBase -= amount * propBase; (implicit since we don't store wBase)

                                foreach (Channel wc in weightChannels)
                                {
                                    if (wc != layerChannel)
                                    {
                                        float prop = wDict[wc] / pool;
                                        float newVal = wDict[wc] - amount * prop;
                                        SetChannel(ref c, wc, newVal);
                                    }
                                }
                                modified = true;
                            }
                        }
                        else
                        {
                            float amount = strength * wThis;
                            SetChannel(ref c, layerChannel, wThis - amount);
                            modified = true;
                        }
                    }
                }

                // Clamp weights
                foreach (Channel wc in weightChannels)
                {
                    float val = GetChannel(c, wc);
                    SetChannel(ref c, wc, Mathf.Clamp01(val));
                }

                // Normalize if sum > 1
                wSum = 0f;
                foreach (Channel wc in weightChannels)
                {
                    wSum += GetChannel(c, wc);
                }
                if (wSum > 1f)
                {
                    float scale = 1f / wSum;
                    foreach (Channel wc in weightChannels)
                    {
                        float val = GetChannel(c, wc);
                        SetChannel(ref c, wc, val * scale);
                    }
                }
            }

            destColors[index] = c;
        }

        private float GetChannel(Color c, Channel ch)
        {
            switch (ch)
            {
                case Channel.R: return c.r;
                case Channel.G: return c.g;
                case Channel.B: return c.b;
                case Channel.A: return c.a;
                default: return 0f;
            }
        }

        private void SetChannel(ref Color c, Channel ch, float val)
        {
            switch (ch)
            {
                case Channel.R: c.r = val; break;
                case Channel.G: c.g = val; break;
                case Channel.B: c.b = val; break;
                case Channel.A: c.a = val; break;
            }
        }

        void FloodFill(bool fill)
        {
            if (workingMesh == null) return;

            Undo.RecordObject(workingMesh, "Flood Fill");
            if (currentStorage != null) Undo.RecordObject(currentStorage, "Flood Fill Storage");

            Color[] colors = workingMesh.colors;
            if (colors == null || colors.Length == 0) return;

            if (paintWorkflow == PaintWorkflow.VertexColor)
            {
                Color target = fill ? vertexPaintColor : vertexEraseColor;
                for (int i = 0; i < colors.Length; i++)
                {
                    Color col = colors[i];
                    switch (vertexColorTarget)
                    {
                        case VertexColorTarget.R:
                            col.r = target.r;
                            break;
                        case VertexColorTarget.G:
                            col.g = target.g;
                            break;
                        case VertexColorTarget.B:
                            col.b = target.b;
                            break;
                        case VertexColorTarget.A:
                            col.a = target.a;
                            break;
                        case VertexColorTarget.RGB:
                            col.r = target.r;
                            col.g = target.g;
                            col.b = target.b;
                            break;
                        default:
                            col = target;
                            break;
                    }
                    colors[i] = col;
                }

                storedColors = colors;
                workingMesh.colors = colors;
                workingMesh.UploadMeshData(false);

                if (currentStorage != null)
                {
                    currentStorage.paintedColors = storedColors;
                    EditorUtility.SetDirty(currentStorage);
                }

                meshIsDirty = true;
                return;
            }

            int layerIdx = (int)activeLayer;
            bool isOverlayMode = layerOverlay[layerIdx];
            Channel layerChannel = layerChannels[layerIdx];

            // Collect weight channels
            List<Channel> weightChannels = new List<Channel>();
            for (int i = 0; i < 5; i++)
            {
                if (!layerOverlay[i] && layerChannels[i] != Channel.None)
                {
                    if (!weightChannels.Contains(layerChannels[i]))
                    {
                        weightChannels.Add(layerChannels[i]);
                    }
                }
            }

            for (int i = 0; i < colors.Length; i++)
            {
                Color col = colors[i];

                if (isOverlayMode)
                {
                    if (layerChannel == Channel.None) continue;
                    float val = fill ? 1.0f : 0.0f;
                    SetChannel(ref col, layerChannel, val);
                }
                else // Blend
                {
                    if (fill)
                    {
                        if (layerChannel == Channel.None) // Fill base: clear all weights
                        {
                            foreach (Channel wc in weightChannels)
                            {
                                SetChannel(ref col, wc, 0f);
                            }
                        }
                        else // Fill layer: set own to 1, others to 0
                        {
                            SetChannel(ref col, layerChannel, 1f);
                            foreach (Channel wc in weightChannels)
                            {
                                if (wc != layerChannel) SetChannel(ref col, wc, 0f);
                            }
                        }
                    }
                    else // Clear
                    {
                        if (layerChannel == Channel.None) // Clear base: nothing
                        {
                        }
                        else
                        {
                            SetChannel(ref col, layerChannel, 0f);
                        }
                    }
                }

                colors[i] = col;
            }

            storedColors = colors;
            workingMesh.colors = colors;
            workingMesh.UploadMeshData(false);

            if (currentStorage != null)
            {
                currentStorage.paintedColors = storedColors;
                EditorUtility.SetDirty(currentStorage);
            }

            meshIsDirty = true;
        }
    }
}
