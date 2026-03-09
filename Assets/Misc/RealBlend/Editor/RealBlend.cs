using UnityEngine;
using UnityEditor;

namespace RealBlend
{
    public class RealBlend : EditorWindow
    {
        // --- Tab Management ---
        public enum PainterTab { Paint = 0, Create = 1, Sculpt = 2 }
        private PainterTab _currentTab = PainterTab.Paint;
        private string[] _tabNames = { "🎨 Paint", "📐 Create (Demo)", "🗿 Sculpt (Demo)" };

        // --- The Sub-Tools ---
        // [SerializeField] ensures the state of the tabs is saved during reloads
        [SerializeField] private VertexPainter_PaintTab _paintTool;
        [SerializeField] private VertexPainter_CreationTab _createTool;
        [SerializeField] private VertexPainter_SculptTab _sculptTool;

        [MenuItem("Tools/RealBlend/Real Blend")]
        public static void ShowWindow()
        {
            RealBlend wnd = GetWindow<RealBlend>("Real Blend");
            wnd.Show();
        }

        void OnEnable()
        {
            // 1. Initialise tools if they don't exist
            if (_paintTool == null) _paintTool = new VertexPainter_PaintTab(this);
            if (_createTool == null) _createTool = new VertexPainter_CreationTab(this);
            if (_sculptTool == null) _sculptTool = new VertexPainter_SculptTab(this);

            // 2. Call OnEnable on the active tool
            GetActiveTool()?.OnEnable();

            // 3. Subscribe to Editor Events
            SceneView.duringSceneGui -= OnSceneGUI; // Prevent double subscription
            SceneView.duringSceneGui += OnSceneGUI;

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable()
        {
            // CRITICAL FIX: We do NOT call tool.OnDisable() or stop the paint mode here.
            // OnDisable is called when maximizing/unmaximizing windows.

            // We only unsubscribe events.
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= OnSelectionChanged;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        void OnDestroy()
        {
            // This is where we actually stop the tool when the window is closed
            _paintTool?.ForceStop();
            _createTool?.OnDisable();
            _sculptTool?.OnDisable();
        }

        void OnUndoRedo()
        {
            GetActiveTool()?.OnUndoRedo();
            SceneView.RepaintAll();
            Repaint();
        }

        void OnSelectionChanged()
        {
            // Force tools to re-evaluate if they should be active
            GetActiveTool()?.OnSelectionChange();
            Repaint();
            SceneView.RepaintAll();
        }

        void OnAfterAssemblyReload()
        {
            // Re-init everything to fix broken textures/references
            if (_paintTool != null) _paintTool.OnReload();
            if (_sculptTool != null) _sculptTool.OnReload();
            if (_createTool != null) _createTool.OnReload();

            Repaint();
        }

        private IVertexPainterTab GetActiveTool()
        {
            switch (_currentTab)
            {
                case PainterTab.Paint: return _paintTool;
                case PainterTab.Create: return _createTool;
                case PainterTab.Sculpt: return _sculptTool;
                default: return _paintTool;
            }
        }

        void OnGUI()
        {
            // --- Styling for the Tabs ---
            // Create a style based on the standard button but larger and bolder
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fixedHeight = 35;       // Taller, more clickable
            tabStyle.fontSize = 13;          // Larger text
            tabStyle.fontStyle = FontStyle.Bold;

            // Remove margins so the buttons touch each other (segmented look)
            tabStyle.margin = new RectOffset(0, 0, 0, 0);

            // Optional: Ensure normal text color is readable
            tabStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // --- Tab Header Container ---
            GUILayout.Space(10);

            // Wrap the tabs in a HelpBox style for a nice background frame
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(2); // Small padding inside the box

                EditorGUI.BeginChangeCheck();

                // Using standard button style base creates a "Segmented" look when margins are 0
                var newTab = (PainterTab)GUILayout.Toolbar((int)_currentTab, _tabNames, tabStyle, GUILayout.Height(35));

                if (EditorGUI.EndChangeCheck())
                {
                    // Transient disable for switching tabs
                    if (_currentTab == PainterTab.Paint) _paintTool.ForceStop();
                    else GetActiveTool()?.OnDisable();

                    _currentTab = newTab;
                    GetActiveTool()?.OnEnable();
                    SceneView.RepaintAll();
                }
                GUILayout.Space(2);
            }

            GUILayout.Space(10);

            // Wrap logic in Try-Catch to prevent entire window from freezing if a single tool crashes
            try
            {
                GetActiveTool()?.OnGUI();
            }
            catch (System.Exception e)
            {
                // If the GUI crashes, we want to know why, but we don't want the window to become unusable
                // Only log if it's a Repaint event to avoid spamming console during Layout
                if (Event.current.type == EventType.Repaint)
                    Debug.LogError("Painter GUI Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!hasFocus) return;
            GetActiveTool()?.OnSceneGUI(sceneView);
        }
    }

}