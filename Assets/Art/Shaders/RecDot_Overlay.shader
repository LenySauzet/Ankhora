Shader "Ankhora/RecDot_Overlay"
{
    // Flat recording-indicator dot for the HUD. Draws a procedural soft-edged circle from the quad UV
    // (so it needs a FULL-RECT sprite, whose UVs span 0..1 — an atlas sub-rect sprite would offset the UV
    // and render a square). It does NOT sample the sprite texture, which also makes it render correctly
    // outside Play Mode (no reliance on the CanvasRenderer binding a per-renderer texture). Flat fill, no
    // gradient; RGB and opacity come from the Graphic vertex colour (RecordingHud sets red/green and pulses
    // the alpha). Drawn on top of all geometry (ZTest Always). Single-pass-instanced (stereo) safe.
    //
    // Quest passthrough note: a translucent overlay lowers the eye-buffer alpha, so a fading dot reveals
    // more real-world passthrough (the "porthole"); RecordingHud keeps a high alpha floor to stay subtle.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}   // unused; kept for the Image inspector
        _Color ("Tint", Color) = (1,1,1,1)
        [Range(0.01,0.4)] _EdgeSoftness ("Edge Softness", Float) = 0.12

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RECDOT_OVERLAY"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _EdgeSoftness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half r = length(IN.uv - 0.5) * 2.0;                       // 0 centre .. 1 circle edge
                half mask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, r);
                return half4(IN.color.rgb, IN.color.a * mask);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
