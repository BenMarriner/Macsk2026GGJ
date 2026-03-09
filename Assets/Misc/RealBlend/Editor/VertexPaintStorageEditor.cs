using UnityEngine;
using UnityEditor;
using System.IO;


namespace RealBlend
{
    [CustomEditor(typeof(VertexPaintStorage))]
    public class VertexPaintStorageEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // CRITICAL: Check if target was destroyed
            if (target == null)
            {
                EditorGUILayout.LabelField("VertexPaintStorage was removed.", EditorStyles.wordWrappedLabel);
                return;
            }

            VertexPaintStorage storage = (VertexPaintStorage)target;

            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Mesh Management", EditorStyles.boldLabel);

            if (storage.hasOriginalData)
                EditorGUILayout.HelpBox($"Ready • {storage.originalVertices.Length} vertices", MessageType.Info);
            else
                EditorGUILayout.HelpBox("No base mesh captured yet", MessageType.Warning);

            GUILayout.Space(10);

            // Global Folder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Bake Folder", VertexPaintStorage.GlobalBakePath);

            if (GUILayout.Button("Change", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Global Bake Folder", VertexPaintStorage.GlobalBakePath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    VertexPaintStorage.GlobalBakePath = "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/");
                }
            }

            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                string full = Path.GetFullPath(VertexPaintStorage.GlobalBakePath);
                if (Directory.Exists(full)) EditorUtility.RevealInFinder(full);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Bake to Asset (Permanent)", GUILayout.Height(45)))
            {
                storage.BakeMeshAsset();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            if (GUILayout.Button("Force Rebuild Mesh", GUILayout.Height(30)))
                storage.RebuildMesh();

            if (storage.hasOriginalData)
            {
                GUILayout.Space(8);
                string preview = $"{VertexPaintStorage.GlobalBakePath}/{storage.gameObject.name}_Baked.asset";
                EditorGUILayout.LabelField("Save path:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(preview, GUILayout.Height(18));
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "After baking:\n" +
                "• Vertex colors are permanently saved\n" +
                "• GameObject uses the new mesh asset\n" +
                "• Component can be safely removed",
                MessageType.Info);
        }
    }
}