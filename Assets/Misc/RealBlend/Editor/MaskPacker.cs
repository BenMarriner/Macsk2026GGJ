// Assets/Editor/HDRPMaskPacker.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RealBlend
{

    public class MaskPacker : EditorWindow
    {
        [Header("Inputs (same resolution preferred - all optional)")]
        public Texture2D metallic; // R channel
        public Texture2D ambientOcclusion; // G channel
        public Texture2D roughnessOrSmoothness; // Used for A channel (Smoothness)
        public Texture2D heightMask; // B channel (optional)

        [Header("Channel Options")]
        public bool useSmoothnessInput = false; // ON = input is Smoothness directly; OFF = input is Roughness (invert to Smoothness)

        [Header("Default Values When Input Missing")]
        [Range(0f, 1f)] public float defaultMetallic = 0f;
        [Range(0f, 1f)] public float defaultAO = 1f;         // 1 = no occlusion
        [Range(0f, 1f)] public float defaultSmoothness = 0.5f; // New: default for Alpha channel
        [Range(0f, 1f)] public float defaultHeightMask = 1f;

        [Header("Output")]
        public string fileName = "MaskMap.png";
        public int overrideWidth = 0;
        public int overrideHeight = 0;

        [Tooltip("If ON, we read pixels as linear (recommended for data maps).")]
        public bool forceLinearRead = true;

        [MenuItem("Tools/RealBlend/Mask Packer")]
        public static void ShowWindow()
        {
            var w = GetWindow<MaskPacker>("Mask Packer");
            w.minSize = new Vector2(360, 440);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Inputs → Mask Map (R=Metallic, G=AO, B=Height Mask, A=Smoothness)", EditorStyles.boldLabel);

            metallic = (Texture2D)EditorGUILayout.ObjectField("Metallic (R, optional)", metallic, typeof(Texture2D), false);
            ambientOcclusion = (Texture2D)EditorGUILayout.ObjectField("Ambient Occlusion (G, optional)", ambientOcclusion, typeof(Texture2D), false);
            roughnessOrSmoothness = (Texture2D)EditorGUILayout.ObjectField(
                useSmoothnessInput ? "Smoothness (A, optional)" : "Roughness (A = 1-R, optional)",
                roughnessOrSmoothness, typeof(Texture2D), false);
            heightMask = (Texture2D)EditorGUILayout.ObjectField("Height Mask (B, optional)", heightMask, typeof(Texture2D), false);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Channel Behavior", EditorStyles.boldLabel);
            useSmoothnessInput = EditorGUILayout.Toggle(
                new GUIContent("Input is Smoothness (not Roughness)", "ON = use input directly as Smoothness\nOFF = invert Roughness → Smoothness"),
                useSmoothnessInput);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Defaults When Texture Missing", EditorStyles.boldLabel);
            defaultMetallic = EditorGUILayout.Slider("Default Metallic", defaultMetallic, 0f, 1f);
            defaultAO = EditorGUILayout.Slider("Default AO", defaultAO, 0f, 1f);
            defaultSmoothness = EditorGUILayout.Slider("Default Smoothness", defaultSmoothness, 0f, 1f);
            defaultHeightMask = EditorGUILayout.Slider("Default Height Mask", defaultHeightMask, 0f, 1f);

            EditorGUILayout.Space(10);
            forceLinearRead = EditorGUILayout.Toggle(new GUIContent("Read as Linear (recommended)"), forceLinearRead);

            overrideWidth = EditorGUILayout.IntField(new GUIContent("Output Width (0 = auto)"), overrideWidth);
            overrideHeight = EditorGUILayout.IntField(new GUIContent("Output Height (0 = auto)"), overrideHeight);

            EditorGUILayout.Space(6);
            fileName = EditorGUILayout.TextField("Output File Name", fileName);

            EditorGUILayout.Space(10);

            // Always allow packing - even with zero inputs!
            if (GUILayout.Button("Pack → Save PNG"))
            {
                PackAndSave();
            }

            EditorGUILayout.HelpBox(
                "Tips:\n• All inputs are optional — missing channels use default values.\n" +
                "• Import grayscale maps with sRGB OFF.\n" +
                "• Output saved in most common folder among assigned textures.\n" +
                "• Blue channel labeled 'Height Mask' for custom shaders.",
                MessageType.Info);
        }

        void PackAndSave()
        {
            // Determine size: use first available texture, or override, or 1024
            int w = FirstNonZero(overrideWidth, metallic?.width, ambientOcclusion?.width, roughnessOrSmoothness?.width, heightMask?.width, 1024);
            int h = FirstNonZero(overrideHeight, metallic?.height, ambientOcclusion?.height, roughnessOrSmoothness?.height, heightMask?.height, 1024);

            var mTex = ReadAsRGBA(metallic, w, h, forceLinearRead);
            var aoTex = ReadAsRGBA(ambientOcclusion, w, h, forceLinearRead);
            var rsTex = ReadAsRGBA(roughnessOrSmoothness, w, h, forceLinearRead);
            var hTex = ReadAsRGBA(heightMask, w, h, forceLinearRead);

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            Color[] outPixels = new Color[w * h];

            Color[] mPixels = mTex?.GetPixels();
            Color[] aoPixels = aoTex?.GetPixels();
            Color[] rsPixels = rsTex?.GetPixels();
            Color[] hPixels = hTex?.GetPixels();

            for (int i = 0; i < outPixels.Length; i++)
            {
                float R = mPixels != null ? mPixels[i].r : defaultMetallic;
                float G = aoPixels != null ? aoPixels[i].r : defaultAO;
                float B = hPixels != null ? hPixels[i].r : defaultHeightMask;

                float smoothness = defaultSmoothness;
                if (rsPixels != null)
                {
                    smoothness = useSmoothnessInput ? rsPixels[i].r : 1f - rsPixels[i].r;
                }
                float A = Mathf.Clamp01(smoothness);

                outPixels[i] = new Color(R, G, B, A);
            }

            outTex.SetPixels(outPixels);
            outTex.Apply(false, false);

            // Smart folder detection
            string folder = GetMostCommonInputFolder() ?? GetSelectedFolderOr("Assets");

            if (!fileName.EndsWith(".png")) fileName += ".png";
            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, fileName));
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
#if UNITY_2020_2_OR_NEWER
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
#endif
                importer.SaveAndReimport();
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            EditorUtility.DisplayDialog("Mask Packer", $"Saved:\n{path}", "OK");
        }

        private string GetMostCommonInputFolder()
        {
            var textures = new List<Texture2D> { metallic, ambientOcclusion, roughnessOrSmoothness, heightMask };
            var validPaths = new List<string>();

            foreach (var tex in textures)
            {
                if (tex != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
                    {
                        string folderPath = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                        validPaths.Add(folderPath);
                    }
                }
            }

            if (validPaths.Count == 0) return null;

            return validPaths
                .GroupBy(f => f)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }

        // --- Helpers ---
        static int FirstNonZero(params int?[] xs)
        {
            foreach (var x in xs) { if (x.HasValue && x.Value > 0) return x.Value; }
            return 1024;
        }

        static Texture2D ReadAsRGBA(Texture2D src, int w, int h, bool linear)
        {
            if (src == null) return null;
            var rtDesc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = !linear
            };
            var rt = RenderTexture.GetTemporary(rtDesc);
            var prev = RenderTexture.active;

            Graphics.Blit(src, rt);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false, false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        static string GetSelectedFolderOr(string fallback)
        {
            string path = fallback;
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var p = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(p)) p = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(p)) { path = p; break; }
            }
            return path.Replace("\\", "/");
        }
    }

}