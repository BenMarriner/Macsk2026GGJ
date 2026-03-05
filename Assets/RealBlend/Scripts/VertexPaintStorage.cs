using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealBlend
{

    [AddComponentMenu("Mesh/Vertex Paint Storage")]
    [ExecuteAlways]
    public class VertexPaintStorage : MonoBehaviour
    {
        [HideInInspector] public Color[] paintedColors;
        [HideInInspector] public Vector3[] currentVertices;
        [HideInInspector] public int[] currentTriangles;
        [HideInInspector] public Vector2[] currentUVs;

        [HideInInspector] public Vector3[] baseVertices;
        [HideInInspector] public int[] baseTriangles;
        [HideInInspector] public Vector2[] baseUVs;
        [HideInInspector] public bool hasBaseData = false;

        public bool hasOriginalData => hasBaseData;
        public Vector3[] originalVertices { get => baseVertices; set => baseVertices = value; }
        public int[] originalTriangles { get => baseTriangles; set => baseTriangles = value; }
        public Vector2[] originalUVs { get => baseUVs; set => baseUVs = value; }

        private Mesh _runtimeMesh;

#if UNITY_EDITOR
        private static readonly string GlobalBakePathKey = "VertexPaintStorage_GlobalBakePath";
        public static string GlobalBakePath
        {
            get => EditorPrefs.GetString(GlobalBakePathKey, "Assets/BakedVertexPaintMeshes");
            set => EditorPrefs.SetString(GlobalBakePathKey, value);
        }
#endif

        void OnEnable()
        {
            if (!HasValidMesh()) RebuildMesh();

#if UNITY_EDITOR
            EditorApplication.delayCall += ApplyColors;
#else
        ApplyColors();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplyColors;
#endif
        }

        bool HasValidMesh()
        {
            var mf = GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null;
        }

        public void RebuildMesh()
        {
            var mf = GetComponent<MeshFilter>();
            if (!mf) return;

            // Use current data if available, otherwise fall back to base
            var verts = currentVertices?.Length > 0 ? currentVertices : baseVertices;
            var tris = currentTriangles?.Length > 0 ? currentTriangles : baseTriangles;
            var uvs = currentUVs?.Length > 0 ? currentUVs : baseUVs;

            // If we still have nothing, we can't build
            if (verts == null || verts.Length == 0) return;

            _runtimeMesh = new Mesh();
            _runtimeMesh.name = "Painted_Runtime_Mesh";
            _runtimeMesh.vertices = verts;
            _runtimeMesh.triangles = tris;
            _runtimeMesh.uv = uvs;

            // Ensure colors match vertex count before assigning
            if (paintedColors != null && paintedColors.Length == verts.Length)
                _runtimeMesh.colors = paintedColors;

            _runtimeMesh.RecalculateNormals();
            _runtimeMesh.RecalculateTangents();
            _runtimeMesh.RecalculateBounds();

            mf.sharedMesh = _runtimeMesh;
            var col = GetComponent<MeshCollider>();
            if (col) col.sharedMesh = _runtimeMesh;
        }

        public void ApplyColors()
        {
            var mf = GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh || paintedColors == null || paintedColors.Length != mf.sharedMesh.vertexCount) return;

            mf.sharedMesh.colors = paintedColors;
            mf.sharedMesh.UploadMeshData(false);
        }

        // --- CRITICAL FIXES START HERE ---

        public void SaveCurrentState(Mesh mesh)
        {
            if (mesh == null) return;
            currentVertices = mesh.vertices;
            currentTriangles = mesh.triangles;
            currentUVs = mesh.uv;
            paintedColors = mesh.colors;
        }

        public void SetBaseTopology(Mesh mesh)
        {
            if (mesh == null) return;
            baseVertices = mesh.vertices;
            baseTriangles = mesh.triangles;
            baseUVs = mesh.uv;
            hasBaseData = true; // This flag enables the Revert button logic

            // Also initialize current state to match base so we aren't null
            SaveCurrentState(mesh);
        }

        public void RestoreOriginalTopology()
        {
            if (!hasBaseData) return;
            currentVertices = baseVertices;
            currentTriangles = baseTriangles;
            currentUVs = baseUVs;
            RebuildMesh();
        }

        // --- CRITICAL FIXES END HERE ---

        public void CaptureOriginals(Mesh mesh) => SetBaseTopology(mesh);

#if UNITY_EDITOR
        public void BakeMeshAsset()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (!mf || !mf.sharedMesh) { Debug.LogWarning("Nothing to bake"); return; }

            string dir = GlobalBakePath;
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = System.IO.Path.GetDirectoryName(dir).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent == "Assets" || string.IsNullOrEmpty(parent) ? "Assets" : parent, folder);
            }

            string fileName = $"{gameObject.name}_Baked.asset";
            string fullPath = System.IO.Path.Combine(dir, fileName).Replace("\\", "/");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            // Create final mesh
            Mesh bakedMesh = Instantiate(mf.sharedMesh);
            bakedMesh.name = gameObject.name + "_Baked";

            AssetDatabase.CreateAsset(bakedMesh, assetPath);
            AssetDatabase.SaveAssets();

            // CRITICAL FIX: Properly assign to both scene and prefab
            MeshFilter mfInstance = mf;
            mfInstance.sharedMesh = bakedMesh;

            var collider = GetComponent<MeshCollider>();
            if (collider) collider.sharedMesh = bakedMesh;

            // Record undo + mark dirty
            Undo.RecordObject(mfInstance, "Bake Vertex Colors");
            if (!Application.isPlaying)
                EditorUtility.SetDirty(mfInstance);

            Debug.Log($"Vertex colors baked permanently: {assetPath}");

            // Ask to remove component
            if (EditorUtility.DisplayDialog("Bake Complete!",
                $"Mesh with vertex colors saved to:\n{assetPath}\n\n" +
                "The GameObject now uses the permanent baked mesh.\n" +
                "Remove VertexPaintStorage component?",
                "Yes", "No"))
            {
                // DESTROY SAFELY (fixes MissingReferenceException)
                DestroyComponentSafely();
            }
        }

        // SAFELY DESTROY (prevents editor crash)
        private void DestroyComponentSafely()
        {
            if (this == null) return;

#if UNITY_EDITOR
            // Delay destruction so Inspector can finish drawing
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    Undo.DestroyObjectImmediate(this);
            };
#else
        Destroy(this);
#endif
        }
#endif
    }
}