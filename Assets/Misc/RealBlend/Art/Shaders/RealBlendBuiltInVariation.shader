Shader "RealBlend/RealBlendBuiltInVariation"
{
    Properties
    {
        [Header(Global)]
        _BlendNoise ("Blend Noise", Float) = 0
        _L12_BlendBridge ("L1/L2 Blend Bridge", Float) = 0.35
        _L12_BlendContrast ("L1/L2 Blend Contrast", Float) = 0.7

        [Header(Base Layer)]
        _Base_Albedo ("Base Albedo", 2D) = "white" {}
        _Base_Normal ("Base Normal", 2D) = "bump" {}
        _Base_MaskMap ("Base Mask (R=Metallic G=AO B=Roughness A=Height)", 2D) = "white" {}
        _Base_Tiling ("Base Tiling", Vector) = (1,1,0,0)
        _Base_Offset ("Base Offset", Vector) = (0,0,0,0)
        _Base_Tint ("Base Tint", Color) = (1,1,1,1)
        _Base_Saturation ("Base Saturation", Float) = 1
        _Base_Brightness ("Base Brightness", Float) = 1
        _Base_Normal_Scale ("Base Normal Scale", Float) = 1
        _Base_Metalic_Scale ("Base Metallic Scale", Float) = 1
        _Base_Smoothness_Scale ("Base Smoothness Scale", Float) = 1
        _Base_AO_Power ("Base AO Strength", Float) = 1
        _Base_HeightBlend ("Base Height Blend", Float) = 1
        _Base_Tint_B ("Base Tint B", Color) = (1,1,1,1)
        _Base_Saturation_B ("Base Saturation B", Float) = 1
        _Base_Brightness_B ("Base Brightness B", Float) = 1
        _Base_NormalScale_B ("Base Normal Scale B", Float) = 1
        _Base_MetallicScale_B ("Base Metallic Scale B", Float) = 1
        _Base_SmoothnessScale_B ("Base Smoothness Scale B", Float) = 1

        [Header(Layer 1)]
        _Layer1_Albedo ("Layer1 Albedo", 2D) = "white" {}
        _Layer1_Normal ("Layer1 Normal", 2D) = "bump" {}
        _Layer1_MaskMap ("Layer1 Mask (R=Metallic G=AO B=Roughness A=Height)", 2D) = "white" {}
        _Layer1_Tiling ("Layer1 Tiling", Vector) = (1,1,0,0)
        _L1_Offset ("Layer1 Offset", Vector) = (0,0,0,0)
        _Layer1_Tint ("Layer1 Tint", Color) = (1,1,1,1)
        _Layer1_Saturation ("Layer1 Saturation", Float) = 1
        _L1_Brightness ("Layer1 Brightness", Float) = 1
        _L1_Normal_Scale ("Layer1 Normal Scale", Float) = 1
        _L1_Metalic_Scale ("Layer1 Metallic Scale", Float) = 1
        _L1_Smoothness_Scale ("Layer1 Smoothness Scale", Float) = 1
        _L1_AO_Power ("Layer1 AO Strength", Float) = 1
        _L1_HeightBlend ("Layer1 Height Blend", Float) = 1
        _Layer1_Opacity ("Layer1 Opacity", Float) = 1
        _Layer1_Contrast ("Layer1 Contrast", Float) = 1
        _L1_Tint_B ("Layer1 Tint B", Color) = (1,1,1,1)
        _L1_Saturation_B ("Layer1 Saturation B", Float) = 1
        _L1_Brightness_B ("Layer1 Brightness B", Float) = 1
        _L1_NormalScale_B ("Layer1 Normal Scale B", Float) = 1
        _L1_MetallicScale_B ("Layer1 Metallic Scale B", Float) = 1
        _L1_SmoothnessScale_B ("Layer1 Smoothness Scale B", Float) = 1

        [Header(Layer 2)]
        _Layer2_Albedo ("Layer2 Albedo", 2D) = "white" {}
        _Layer2_Normal ("Layer2 Normal", 2D) = "bump" {}
        _Layer2_MaskMap ("Layer2 Mask (R=Metallic G=AO B=Roughness A=Height)", 2D) = "white" {}
        _Layer2_Tiling ("Layer2 Tiling", Vector) = (1,1,0,0)
        _Layer2_Offset ("Layer2 Offset", Vector) = (0,0,0,0)
        _Layer2_Tint ("Layer2 Tint", Color) = (1,1,1,1)
        _Layer2_Saturation ("Layer2 Saturation", Float) = 1
        _L2_Brightness ("Layer2 Brightness", Float) = 1
        _L2_Normal_Scale ("Layer2 Normal Scale", Float) = 1
        _L2_Metalic_Scale ("Layer2 Metallic Scale", Float) = 1
        _L2_Smoothness_Scale ("Layer2 Smoothness Scale", Float) = 1
        _L2_AO_Power ("Layer2 AO Strength", Float) = 1
        _L2_HeightBlend ("Layer2 Height Blend", Float) = 1
        _Layer2_Opacity ("Layer2 Opacity", Float) = 1
        _Layer2_Contrast ("Layer2 Contrast", Float) = 1
        _L2_Tint_B ("Layer2 Tint B", Color) = (1,1,1,1)
        _L2_Saturation_B ("Layer2 Saturation B", Float) = 1
        _L2_Brightness_B ("Layer2 Brightness B", Float) = 1
        _L2_NormalScale_B ("Layer2 Normal Scale B", Float) = 1
        _L2_MetallicScale_B ("Layer2 Metallic Scale B", Float) = 1
        _L2_SmoothnessScale_B ("Layer2 Smoothness Scale B", Float) = 1

        [Header(Wetness)]
        _Wet_Color ("Wet Tint", Color) = (1,1,1,1)
        _Wet_Smoothness ("Wet Smoothness", Float) = 1
        _Wet_Metallic ("Wet Metallic", Float) = 0
        _Wet_AO_Power ("Wet AO Influence", Float) = 0
        _Wet_Opacity ("Wet Opacity", Float) = 1

    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow

        #include "UnityCG.cginc"
        #include "PolybrushMixer.cginc"

        sampler2D _Base_Albedo;
        sampler2D _Base_Normal;
        sampler2D _Base_MaskMap;
        sampler2D _Layer1_Albedo;
        sampler2D _Layer1_Normal;
        sampler2D _Layer1_MaskMap;
        sampler2D _Layer2_Albedo;
        sampler2D _Layer2_Normal;
        sampler2D _Layer2_MaskMap;

        float4 _Base_Tiling;
        float4 _Base_Offset;
        float4 _Layer1_Tiling;
        float4 _L1_Offset;
        float4 _Layer2_Tiling;
        float4 _Layer2_Offset;

        float _BlendNoise;
        float _L12_BlendBridge;
        float _L12_BlendContrast;

        float4 _Base_Tint;
        float _Base_Saturation;
        float _Base_Brightness;
        float _Base_Normal_Scale;
        float _Base_Metalic_Scale;
        float _Base_Smoothness_Scale;
        float _Base_AO_Power;
        float _Base_HeightBlend;

        float4 _Base_Tint_B;
        float _Base_Saturation_B;
        float _Base_Brightness_B;
        float _Base_NormalScale_B;
        float _Base_MetallicScale_B;
        float _Base_SmoothnessScale_B;

        float4 _Layer1_Tint;
        float _Layer1_Saturation;
        float _L1_Brightness;
        float _L1_Normal_Scale;
        float _L1_Metalic_Scale;
        float _L1_Smoothness_Scale;
        float _L1_AO_Power;
        float _L1_HeightBlend;
        float _Layer1_Opacity;
        float _Layer1_Contrast;

        float4 _L1_Tint_B;
        float _L1_Saturation_B;
        float _L1_Brightness_B;
        float _L1_NormalScale_B;
        float _L1_MetallicScale_B;
        float _L1_SmoothnessScale_B;

        float4 _Layer2_Tint;
        float _Layer2_Saturation;
        float _L2_Brightness;
        float _L2_Normal_Scale;
        float _L2_Metalic_Scale;
        float _L2_Smoothness_Scale;
        float _L2_AO_Power;
        float _L2_HeightBlend;
        float _Layer2_Opacity;
        float _Layer2_Contrast;

        float4 _L2_Tint_B;
        float _L2_Saturation_B;
        float _L2_Brightness_B;
        float _L2_NormalScale_B;
        float _L2_MetallicScale_B;
        float _L2_SmoothnessScale_B;

        float4 _Wet_Color;
        float _Wet_Smoothness;
        float _Wet_Metallic;
        float _Wet_AO_Power;
        float _Wet_Opacity;

        struct Input
        {
            float2 uv_Base_Albedo;
            float4 color : COLOR;
        };

        float2 ApplyUv(float2 uv, float4 tiling, float4 offsetVal)
        {
            return uv * tiling.xy + offsetVal.xy;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uvBase = ApplyUv(IN.uv_Base_Albedo, _Base_Tiling, _Base_Offset);
            float2 uvL1 = ApplyUv(IN.uv_Base_Albedo, _Layer1_Tiling, _L1_Offset);
            float2 uvL2 = ApplyUv(IN.uv_Base_Albedo, _Layer2_Tiling, _Layer2_Offset);

            float4 vertexMasks = saturate(IN.color);
            float overlap12 = saturate(vertexMasks.g * vertexMasks.b);
            float bridge = saturate(_L12_BlendBridge);

            // Boost overlap region so L1/L2 meet instead of leaving a visual gap.
            vertexMasks.g = saturate(vertexMasks.g + overlap12 * bridge * (1.0 - vertexMasks.g));
            vertexMasks.b = saturate(vertexMasks.b + overlap12 * bridge * (1.0 - vertexMasks.b));

            // Scale contrast only where L1 and L2 overlap (lower than 1 softens blend).
            float overlapContrast = lerp(1.0, _L12_BlendContrast, overlap12);
            float l1ContrastFinal = _Layer1_Contrast * overlapContrast;
            float l2ContrastFinal = _Layer2_Contrast * overlapContrast;

            float4 baseAlbedoTex = tex2D(_Base_Albedo, uvBase);
            float3 baseNormalTex = UnpackNormal(tex2D(_Base_Normal, uvBase));
            float4 baseMaskTex = tex2D(_Base_MaskMap, uvBase);

            float4 l1AlbedoTex = tex2D(_Layer1_Albedo, uvL1);
            float3 l1NormalTex = UnpackNormal(tex2D(_Layer1_Normal, uvL1));
            float4 l1MaskTex = tex2D(_Layer1_MaskMap, uvL1);

            float4 l2AlbedoTex = tex2D(_Layer2_Albedo, uvL2);
            float3 l2NormalTex = UnpackNormal(tex2D(_Layer2_Normal, uvL2));
            float4 l2MaskTex = tex2D(_Layer2_MaskMap, uvL2);

            float baseSmoothness = saturate(1.0 - baseMaskTex.b);
            float l1Smoothness = saturate(1.0 - l1MaskTex.b);
            float l2Smoothness = saturate(1.0 - l2MaskTex.b);

            float3 outAlbedo;
            float3 outNormal;
            float outMetallic;
            float outSmoothness;
            float outOcclusion;

            PolybrushMixer_float(
                vertexMasks,
                _BlendNoise,

                baseAlbedoTex.rgb,
                baseNormalTex,
                baseMaskTex.r,
                baseSmoothness,
                baseMaskTex.g,
                baseMaskTex.a,
                _Base_Tint.rgb,
                _Base_Saturation,
                _Base_Brightness,
                _Base_Normal_Scale,
                _Base_Metalic_Scale,
                _Base_Smoothness_Scale,
                _Base_HeightBlend,
                0.0,
                _Base_Tint_B.rgb,
                _Base_Saturation_B,
                _Base_Brightness_B,
                _Base_NormalScale_B,
                _Base_MetallicScale_B,
                _Base_SmoothnessScale_B,
                _Base_HeightBlend,
                0.0,
                _Base_AO_Power,

                l1AlbedoTex.rgb,
                l1NormalTex,
                l1MaskTex.r,
                l1Smoothness,
                l1MaskTex.g,
                l1MaskTex.a,
                _Layer1_Opacity,
                l1ContrastFinal,
                _Layer1_Tint.rgb,
                _Layer1_Saturation,
                _L1_Brightness,
                _L1_Normal_Scale,
                _L1_Metalic_Scale,
                _L1_Smoothness_Scale,
                _L1_HeightBlend,
                0.0,
                _L1_Tint_B.rgb,
                _L1_Saturation_B,
                _L1_Brightness_B,
                _L1_NormalScale_B,
                _L1_MetallicScale_B,
                _L1_SmoothnessScale_B,
                _L1_HeightBlend,
                0.0,
                _L1_AO_Power,

                l2AlbedoTex.rgb,
                l2NormalTex,
                l2MaskTex.r,
                l2Smoothness,
                l2MaskTex.g,
                l2MaskTex.a,
                _Layer2_Opacity,
                l2ContrastFinal,
                _Layer2_Tint.rgb,
                _Layer2_Saturation,
                _L2_Brightness,
                _L2_Normal_Scale,
                _L2_Metalic_Scale,
                _L2_Smoothness_Scale,
                _L2_HeightBlend,
                0.0,
                _L2_Tint_B.rgb,
                _L2_Saturation_B,
                _L2_Brightness_B,
                _L2_NormalScale_B,
                _L2_MetallicScale_B,
                _L2_SmoothnessScale_B,
                _L2_HeightBlend,
                0.0,
                _L2_AO_Power,

                _Wet_Color.rgb,
                _Wet_Smoothness,
                _Wet_Metallic,
                _Wet_AO_Power,
                _Wet_Opacity,

                outAlbedo,
                outNormal,
                outMetallic,
                outSmoothness,
                outOcclusion
            );

            o.Albedo = outAlbedo;
            o.Normal = normalize(outNormal);
            o.Metallic = saturate(outMetallic);
            o.Smoothness = saturate(outSmoothness);
            o.Occlusion = saturate(outOcclusion);
            o.Alpha = 1.0;
        }
        ENDCG
    }

    Fallback "Standard"
}


