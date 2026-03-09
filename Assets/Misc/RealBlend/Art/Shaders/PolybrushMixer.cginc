// Helper for Saturation
float3 AdjustSaturation(float3 color, float saturation)
{
    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    return lerp(luma.xxx, color, saturation);
}

// Helper to Scale Normal Map Intensity
float3 ScaleNormal(float3 normal, float scale)
{
    normal.xy *= scale;
    return normalize(normal);
}

// Helper to blend scalar values based on variation mask
float Var1(float A, float B, float mask) 
{ 
    return lerp(A, B, mask); 
}

// Helper to blend vector values based on variation mask
float3 Var3(float3 A, float3 B, float mask) 
{ 
    return lerp(A, B, mask); 
}

void PolybrushMixer_float(
    // --- Global Inputs ---
    float4 VertexColorMasks,
    float BlendNoise,           // AAA Noise Breakup
    
    // --- Base Layer Inputs ---
    float3 Base_Albedo,
    float3 Base_Normal,
    float Base_Metallic,
    float Base_Smoothness,
    float Base_Occlusion,
    float Base_Height,
    // Set A (Standard)
    float3 Base_Tint,       
    float Base_Saturation,
    float Base_Brightness,      
    float Base_NormalScale,     
    float Base_MetallicScale,  
    float Base_SmoothnessScale,
    float Base_HeightStr,       // Shared -> Now Set A
    float Base_HeightOffset,    // Shared -> Now Set A
    // Set B (Variation)
    float3 Base_Tint_B,       
    float Base_Saturation_B,
    float Base_Brightness_B,    
    float Base_NormalScale_B,     
    float Base_MetallicScale_B,  
    float Base_SmoothnessScale_B,
    float Base_HeightStr_B,     // NEW: Height Strength B
    float Base_HeightOffset_B,  // NEW: Height Offset B
    // Shared
    float Base_OcclusionStr,    
    
    // --- Layer 1 Inputs ---
    float3 L1_Albedo,
    float3 L1_Normal,
    float L1_Metallic,
    float L1_Smoothness,
    float L1_Occlusion,
    float L1_Height,
    float L1_Opacity,
    float L1_Contrast,
    // Set A
    float3 L1_Tint,         
    float L1_Saturation,
    float L1_Brightness,        
    float L1_NormalScale,       
    float L1_MetallicScale,     
    float L1_SmoothnessScale,
    float L1_HeightStr,         // Shared -> Now Set A
    float L1_HeightOffset,      // Shared -> Now Set A
    // Set B (Variation)
    float3 L1_Tint_B,         
    float L1_Saturation_B,
    float L1_Brightness_B,      
    float L1_NormalScale_B,       
    float L1_MetallicScale_B,     
    float L1_SmoothnessScale_B, 
    float L1_HeightStr_B,       // NEW
    float L1_HeightOffset_B,    // NEW
    // Shared
    float L1_OcclusionStr,       
    
    // --- Layer 2 Inputs ---
    float3 L2_Albedo,
    float3 L2_Normal,
    float L2_Metallic,
    float L2_Smoothness,
    float L2_Occlusion,
    float L2_Height,
    float L2_Opacity,
    float L2_Contrast,
    // Set A
    float3 L2_Tint,         
    float L2_Saturation,
    float L2_Brightness,        
    float L2_NormalScale,       
    float L2_MetallicScale,     
    float L2_SmoothnessScale,   
    float L2_HeightStr,         // Shared -> Now Set A
    float L2_HeightOffset,      // Shared -> Now Set A
    // Set B (Variation)
    float3 L2_Tint_B,         
    float L2_Saturation_B,
    float L2_Brightness_B,      
    float L2_NormalScale_B,       
    float L2_MetallicScale_B,     
    float L2_SmoothnessScale_B, 
    float L2_HeightStr_B,       // NEW
    float L2_HeightOffset_B,    // NEW
    // Shared
    float L2_OcclusionStr,       
    
    // --- Wetness Inputs ---
    float3 Wet_Tint,     
    float Wet_Smoothness, 
    float Wet_Metallic,  
    float Wet_AoInfluence, 
    float Wet_Opacity,   
    
    // --- Outputs ---
    out float3 Out_Albedo,
    out float3 Out_Normal,
    out float Out_Metallic,
    out float Out_Smoothness,
    out float Out_Occlusion
)
{
    // 0. Setup Variation Mask (Using Red Channel)
    float varMask = VertexColorMasks.r;

    // --- PRE-PROCESS: BASE ---
    // 1. Blend Property Sets (A vs B)
    float3 b_Tint = Var3(Base_Tint, Base_Tint_B, varMask);
    float b_Sat   = Var1(Base_Saturation, Base_Saturation_B, varMask);
    float b_Brit  = Var1(Base_Brightness, Base_Brightness_B, varMask); 
    float b_NormS = Var1(Base_NormalScale, Base_NormalScale_B, varMask);
    float b_MetS  = Var1(Base_MetallicScale, Base_MetallicScale_B, varMask);
    float b_SmoS  = Var1(Base_SmoothnessScale, Base_SmoothnessScale_B, varMask);
    
    // NEW: Blend Height Props
    float b_HStr  = Var1(Base_HeightStr, Base_HeightStr_B, varMask);
    float b_HOff  = Var1(Base_HeightOffset, Base_HeightOffset_B, varMask);

    // 2. Apply Logic using Blended Props
    float3 baseCol = AdjustSaturation(Base_Albedo, b_Sat) * b_Tint * b_Brit;
    float3 baseNorm = ScaleNormal(Base_Normal, b_NormS);
    float baseMet = Base_Metallic * b_MetS;
    float baseSmooth = Base_Smoothness * b_SmoS;
    
    float baseOcc = lerp(1.0, Base_Occlusion, Base_OcclusionStr);
    
    // Use Blended Height Params
    float baseH = (Base_Height * b_HStr) + b_HOff;

    // --- PRE-PROCESS: LAYER 1 ---
    float3 l1_Tint = Var3(L1_Tint, L1_Tint_B, varMask);
    float l1_Sat   = Var1(L1_Saturation, L1_Saturation_B, varMask);
    float l1_Brit  = Var1(L1_Brightness, L1_Brightness_B, varMask); 
    float l1_NormS = Var1(L1_NormalScale, L1_NormalScale_B, varMask);
    float l1_MetS  = Var1(L1_MetallicScale, L1_MetallicScale_B, varMask);
    float l1_SmoS  = Var1(L1_SmoothnessScale, L1_SmoothnessScale_B, varMask);

    // NEW: Blend Height Props
    float l1_HStr  = Var1(L1_HeightStr, L1_HeightStr_B, varMask);
    float l1_HOff  = Var1(L1_HeightOffset, L1_HeightOffset_B, varMask);

    float3 l1Col = AdjustSaturation(L1_Albedo, l1_Sat) * l1_Tint * l1_Brit;
    float3 l1Norm = ScaleNormal(L1_Normal, l1_NormS);
    float l1Met = L1_Metallic * l1_MetS;
    float l1Smooth = L1_Smoothness * l1_SmoS;
    
    float l1Occ = lerp(1.0, L1_Occlusion, L1_OcclusionStr);
    
    // Use Blended Height Params
    float l1H = (L1_Height * l1_HStr) + l1_HOff;

    // --- PRE-PROCESS: LAYER 2 ---
    float3 l2_Tint = Var3(L2_Tint, L2_Tint_B, varMask);
    float l2_Sat   = Var1(L2_Saturation, L2_Saturation_B, varMask);
    float l2_Brit  = Var1(L2_Brightness, L2_Brightness_B, varMask); 
    float l2_NormS = Var1(L2_NormalScale, L2_NormalScale_B, varMask);
    float l2_MetS  = Var1(L2_MetallicScale, L2_MetallicScale_B, varMask);
    float l2_SmoS  = Var1(L2_SmoothnessScale, L2_SmoothnessScale_B, varMask);

    // NEW: Blend Height Props
    float l2_HStr  = Var1(L2_HeightStr, L2_HeightStr_B, varMask);
    float l2_HOff  = Var1(L2_HeightOffset, L2_HeightOffset_B, varMask);

    float3 l2Col = AdjustSaturation(L2_Albedo, l2_Sat) * l2_Tint * l2_Brit;
    float3 l2Norm = ScaleNormal(L2_Normal, l2_NormS);
    float l2Met = L2_Metallic * l2_MetS;
    float l2Smooth = L2_Smoothness * l2_SmoS;
    
    float l2Occ = lerp(1.0, L2_Occlusion, L2_OcclusionStr);
    
    // Use Blended Height Params
    float l2H = (L2_Height * l2_HStr) + l2_HOff;

    // ---------------------------------------------------------
    // MIXING LOGIC (AAA Height Blend)
    // ---------------------------------------------------------

    // 1. Mix Base and Layer 1
    float maskL1 = VertexColorMasks.g * L1_Opacity;
    float heightDiff1 = l1H - baseH;
    
    // AAA FIX: Adding noise to the blend edge
    float blendInput1 = heightDiff1 + (maskL1 * 2.0 - 1.0) + (BlendNoise * 0.1); 
    float blendFactor1 = saturate(blendInput1 * L1_Contrast + 0.5);
    
    float3 mixAlbedo = lerp(baseCol, l1Col, blendFactor1);
    float3 mixNormal = normalize(lerp(baseNorm, l1Norm, blendFactor1)); 
    float mixMet = lerp(baseMet, l1Met, blendFactor1);
    float mixSmooth = lerp(baseSmooth, l1Smooth, blendFactor1);
    float mixAo = lerp(baseOcc, l1Occ, blendFactor1);
    float currentHeight = lerp(baseH, l1H, blendFactor1);

    // 2. Mix Result and Layer 2
    float maskL2 = VertexColorMasks.b * L2_Opacity;
    float heightDiff2 = l2H - currentHeight;
    
    // AAA FIX: Adding noise to the blend edge
    float blendInput2 = heightDiff2 + (maskL2 * 2.0 - 1.0) + (BlendNoise * 0.1);
    float blendFactor2 = saturate(blendInput2 * L2_Contrast + 0.5);

    mixAlbedo = lerp(mixAlbedo, l2Col, blendFactor2);
    mixNormal = normalize(lerp(mixNormal, l2Norm, blendFactor2));
    mixMet = lerp(mixMet, l2Met, blendFactor2);
    mixSmooth = lerp(mixSmooth, l2Smooth, blendFactor2);
    mixAo = lerp(mixAo, l2Occ, blendFactor2);
    
    // 3. Apply Wetness (AAA PBR Logic)
    // ---------------------------------------------------------
    float wetInput = saturate(VertexColorMasks.a * Wet_Opacity);
    
    // Phase 1: Porosity
    float porosity = saturate(wetInput * 2.0); 
    float darkening = lerp(1.0, 0.4, porosity * 0.5); 
    mixAlbedo *= darkening;
    
    // Phase 2: Puddles
    float puddle = saturate((wetInput - 0.4) * 1.66); 
    
    mixAlbedo = lerp(mixAlbedo, mixAlbedo * Wet_Tint, puddle); 
    mixNormal = normalize(lerp(mixNormal, float3(0, 0, 1), puddle));
    mixSmooth = lerp(mixSmooth, Wet_Smoothness, puddle);
    mixMet = lerp(mixMet, Wet_Metallic, puddle);
    mixAo = lerp(mixAo, saturate(mixAo * (1.0 - Wet_AoInfluence)), puddle);

    // Final Output
    Out_Albedo = mixAlbedo;
    Out_Normal = mixNormal;
    Out_Metallic = mixMet;
    Out_Smoothness = mixSmooth;
    Out_Occlusion = mixAo;
}