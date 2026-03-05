Shader "RealBlend/Vertex Color Preview"
{
    Properties
    {
        _Opacity ("Overlay Opacity", Range(0, 1)) = 0.85
        _ShowAlpha ("Show Alpha (0 RGB / 1 A)", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            float _Opacity;
            float _ShowAlpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed alphaChannel = i.color.a;
                fixed3 rgb = i.color.rgb;
                fixed3 alphaGray = alphaChannel.xxx;
                fixed3 outColor = lerp(rgb, alphaGray, saturate(_ShowAlpha));
                return fixed4(outColor, _Opacity);
            }
            ENDCG
        }
    }

    FallBack Off
}
