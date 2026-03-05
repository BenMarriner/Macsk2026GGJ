#ifndef POLYBRUSH_MIXER5_GBAR_INCLUDED
#define POLYBRUSH_MIXER5_GBAR_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ------------------------------------------------------------
// Helpers
// ------------------------------------------------------------
float3 AdjustSaturation(float3 color, float saturation)
{
    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    return lerp(luma.xxx, color, saturation);
}

float3 ScaleNormal(float3 n, float scale)
{
    n.xy *= scale;
    return normalize(n);
}

float3 SafeNrm(float3 n)
{
    float len = max(1e-5, length(n));
    return n / len;
}


void PolybrushMixer5_float(
    // --- Global Inputs ---
    float4 VertexColorMasks,
    float  BlendNoise, // optional breakup (0..1)

    // --- Base ---
    float3 _Base_Albedo,
    float3 _Base_Normal,
    float  _Base_Metallic,
    float  _Base_Smoothness,
    float  _Base_Occlusion,
    float  _Base_Height,

    float3 _Base_Tint,
    float  _Base_Saturation,
    float  _Base_Brightness,
    float  _Base_NormalScale,
    float  _Base_MetallicScale,
    float  _Base_SmoothnessScale,
    float  _Base_OcclusionStr,
    float  _Base_HeightStr,
    float  _Base_HeightOffset,

    // --- Layer 1 (G) ---
    float3 _Layer1_Albedo,
    float3 _Layer1_Normal,
    float  _Layer1_Metallic,
    float  _Layer1_Smoothness,
    float  _Layer1_Occlusion,
    float  _Layer1_Height,

    float  _Layer1_Opacity,
    float  _Layer1_Contrast, // height edge sharpness
    float3 _Layer1_Tint,
    float  _Layer1_Saturation,
    float  _Layer1_Brightness,
    float  _Layer1_NormalScale,
    float  _Layer1_MetallicScale,
    float  _Layer1_SmoothnessScale,
    float  _Layer1_OcclusionStr,
    float  _Layer1_HeightStr,
    float  _Layer1_HeightOffset,

    // --- Layer 2 (B) ---
    float3 _Layer2_Albedo,
    float3 _Layer2_Normal,
    float  _Layer2_Metallic,
    float  _Layer2_Smoothness,
    float  _Layer2_Occlusion,
    float  _Layer2_Height,

    float  _Layer2_Opacity,
    float  _Layer2_Contrast,
    float3 _Layer2_Tint,
    float  _Layer2_Saturation,
    float  _Layer2_Brightness,
    float  _Layer2_NormalScale,
    float  _Layer2_MetallicScale,
    float  _Layer2_SmoothnessScale,
    float  _Layer2_OcclusionStr,
    float  _Layer2_HeightStr,
    float  _Layer2_HeightOffset,

    // --- Layer 3 (A) ---
    float3 _Layer3_Albedo,
    float3 _Layer3_Normal,
    float  _Layer3_Metallic,
    float  _Layer3_Smoothness,
    float  _Layer3_Occlusion,
    float  _Layer3_Height,

    float  _Layer3_Opacity,
    float  _Layer3_Contrast,
    float3 _Layer3_Tint,
    float  _Layer3_Saturation,
    float  _Layer3_Brightness,
    float  _Layer3_NormalScale,
    float  _Layer3_MetallicScale,
    float  _Layer3_SmoothnessScale,
    float  _Layer3_OcclusionStr,
    float  _Layer3_HeightStr,
    float  _Layer3_HeightOffset,

    // --- Layer 4 (R) ---
    float3 _Layer4_Albedo,
    float3 _Layer4_Normal,
    float  _Layer4_Metallic,
    float  _Layer4_Smoothness,
    float  _Layer4_Occlusion,
    float  _Layer4_Height,

    float  _Layer4_Opacity,
    float  _Layer4_Contrast,
    float3 _Layer4_Tint,
    float  _Layer4_Saturation,
    float  _Layer4_Brightness,
    float  _Layer4_NormalScale,
    float  _Layer4_MetallicScale,
    float  _Layer4_SmoothnessScale,
    float  _Layer4_OcclusionStr,
    float  _Layer4_HeightStr,
    float  _Layer4_HeightOffset,

    // --- Outputs ---
    out float3 Out_Albedo,
    out float3 Out_Normal,
    out float  Out_Metallic,
    out float  Out_Smoothness,
    out float  Out_Occlusion
)
{
    // ------------------------------------------------------------
    // 0) Weights (GBAR mapping)
    // ------------------------------------------------------------
    float wL1 = saturate(VertexColorMasks.g) * _Layer1_Opacity; // G
    float wL2 = saturate(VertexColorMasks.b) * _Layer2_Opacity; // B
    float wL3 = saturate(VertexColorMasks.a) * _Layer3_Opacity; // A
    float wL4 = saturate(VertexColorMasks.r) * _Layer4_Opacity; // R

    float sumW  = wL1 + wL2 + wL3 + wL4;
    float wBase = saturate(1.0 - sumW);

    // Normalize so energy stays consistent even if sumW > 1
    float denom = max(1e-5, (wBase + sumW));
    wBase /= denom;
    wL1   /= denom;
    wL2   /= denom;
    wL3   /= denom;
    wL4   /= denom;

    // ------------------------------------------------------------
    // 1) Preprocess each layer (tint/sat/bri + normal scale + PBR scales)
    // ------------------------------------------------------------
    float3 baseCol = AdjustSaturation(_Base_Albedo, _Base_Saturation) * _Base_Tint * _Base_Brightness;
    float3 baseNrm = ScaleNormal(_Base_Normal, _Base_NormalScale);
    float  baseMet = _Base_Metallic   * _Base_MetallicScale;
    float  baseSmo = _Base_Smoothness * _Base_SmoothnessScale;
    float  baseOcc = lerp(1.0, _Base_Occlusion, _Base_OcclusionStr);
    float  baseH   = (_Base_Height * _Base_HeightStr) + _Base_HeightOffset;

    float3 l1Col = AdjustSaturation(_Layer1_Albedo, _Layer1_Saturation) * _Layer1_Tint * _Layer1_Brightness;
    float3 l1Nrm = ScaleNormal(_Layer1_Normal, _Layer1_NormalScale);
    float  l1Met = _Layer1_Metallic   * _Layer1_MetallicScale;
    float  l1Smo = _Layer1_Smoothness * _Layer1_SmoothnessScale;
    float  l1Occ = lerp(1.0, _Layer1_Occlusion, _Layer1_OcclusionStr);
    float  l1H   = (_Layer1_Height * _Layer1_HeightStr) + _Layer1_HeightOffset;

    float3 l2Col = AdjustSaturation(_Layer2_Albedo, _Layer2_Saturation) * _Layer2_Tint * _Layer2_Brightness;
    float3 l2Nrm = ScaleNormal(_Layer2_Normal, _Layer2_NormalScale);
    float  l2Met = _Layer2_Metallic   * _Layer2_MetallicScale;
    float  l2Smo = _Layer2_Smoothness * _Layer2_SmoothnessScale;
    float  l2Occ = lerp(1.0, _Layer2_Occlusion, _Layer2_OcclusionStr);
    float  l2H   = (_Layer2_Height * _Layer2_HeightStr) + _Layer2_HeightOffset;

    float3 l3Col = AdjustSaturation(_Layer3_Albedo, _Layer3_Saturation) * _Layer3_Tint * _Layer3_Brightness;
    float3 l3Nrm = ScaleNormal(_Layer3_Normal, _Layer3_NormalScale);
    float  l3Met = _Layer3_Metallic   * _Layer3_MetallicScale;
    float  l3Smo = _Layer3_Smoothness * _Layer3_SmoothnessScale;
    float  l3Occ = lerp(1.0, _Layer3_Occlusion, _Layer3_OcclusionStr);
    float  l3H   = (_Layer3_Height * _Layer3_HeightStr) + _Layer3_HeightOffset;

    float3 l4Col = AdjustSaturation(_Layer4_Albedo, _Layer4_Saturation) * _Layer4_Tint * _Layer4_Brightness;
    float3 l4Nrm = ScaleNormal(_Layer4_Normal, _Layer4_NormalScale);
    float  l4Met = _Layer4_Metallic   * _Layer4_MetallicScale;
    float  l4Smo = _Layer4_Smoothness * _Layer4_SmoothnessScale;
    float  l4Occ = lerp(1.0, _Layer4_Occlusion, _Layer4_OcclusionStr);
    float  l4H   = (_Layer4_Height * _Layer4_HeightStr) + _Layer4_HeightOffset;

    // ------------------------------------------------------------
    // 2) Progressive height blend (base -> L1 -> L2 -> L3 -> L4)
    // ------------------------------------------------------------
    float3 col  = baseCol;
    float3 nrm  = baseNrm;
    float  met  = baseMet;
    float  smo  = baseSmo;
    float  occ  = baseOcc;
    float  curH = baseH;

    float n = (BlendNoise * 0.1);

    // Blend Layer 1 (G)
    {
        float mask = wL1;
        float blendInput = (l1H - curH) + (mask * 2.0 - 1.0) + n;
        float f = saturate(blendInput * _Layer1_Contrast + 0.5);

        col  = lerp(col,  l1Col, f);
        nrm  = SafeNrm(lerp(nrm, l1Nrm, f));
        met  = lerp(met,  l1Met, f);
        smo  = lerp(smo,  l1Smo, f);
        occ  = lerp(occ,  l1Occ, f);
        curH = lerp(curH, l1H,   f);
    }

    // Blend Layer 2 (B)
    {
        float mask = wL2;
        float blendInput = (l2H - curH) + (mask * 2.0 - 1.0) + n;
        float f = saturate(blendInput * _Layer2_Contrast + 0.5);

        col  = lerp(col,  l2Col, f);
        nrm  = SafeNrm(lerp(nrm, l2Nrm, f));
        met  = lerp(met,  l2Met, f);
        smo  = lerp(smo,  l2Smo, f);
        occ  = lerp(occ,  l2Occ, f);
        curH = lerp(curH, l2H,   f);
    }

    // Blend Layer 3 (A)
    {
        float mask = wL3;
        float blendInput = (l3H - curH) + (mask * 2.0 - 1.0) + n;
        float f = saturate(blendInput * _Layer3_Contrast + 0.5);

        col  = lerp(col,  l3Col, f);
        nrm  = SafeNrm(lerp(nrm, l3Nrm, f));
        met  = lerp(met,  l3Met, f);
        smo  = lerp(smo,  l3Smo, f);
        occ  = lerp(occ,  l3Occ, f);
        curH = lerp(curH, l3H,   f);
    }

    // Blend Layer 4 (R)
    {
        float mask = wL4;
        float blendInput = (l4H - curH) + (mask * 2.0 - 1.0) + n;
        float f = saturate(blendInput * _Layer4_Contrast + 0.5);

        col  = lerp(col,  l4Col, f);
        nrm  = SafeNrm(lerp(nrm, l4Nrm, f));
        met  = lerp(met,  l4Met, f);
        smo  = lerp(smo,  l4Smo, f);
        occ  = lerp(occ,  l4Occ, f);
        curH = lerp(curH, l4H,   f);
    }

    // ------------------------------------------------------------
    // 3) Enforce base leftover (optional but keeps true "unpainted base")
    // ------------------------------------------------------------
    col = lerp(baseCol, col, 1.0 - wBase);
    nrm = SafeNrm(lerp(baseNrm, nrm, 1.0 - wBase));
    met = lerp(baseMet, met, 1.0 - wBase);
    smo = lerp(baseSmo, smo, 1.0 - wBase);
    occ = lerp(baseOcc, occ, 1.0 - wBase);

    // Outputs
    Out_Albedo     = col;
    Out_Normal     = nrm;
    Out_Metallic   = met;
    Out_Smoothness = smo;
    Out_Occlusion  = occ;
}

#endif // POLYBRUSH_MIXER5_GBAR_INCLUDED
