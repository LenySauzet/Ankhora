// Ankhora floor grid — URP Unlit, transparent, single-pass-instanced (stereo) safe.
// An "infinite" world-space grid that fades out in a circle around the player, with major
// cells subdivided into 4 by thinner minor lines. Shown only in VR: it dissolves as the
// global _AnkhoraMrAmount (0 = VR, 1 = MR, driven by the passthrough transition) rises.
// Cheap on Quest: unlit, math-only lines, fully-transparent fragments are discarded.
Shader "Ankhora/FloorGrid"
{
    Properties
    {
        _BaseColor      ("Base Color", Color)        = (0.961, 0.961, 0.996, 0.85) // #F5F5FE
        _LineColor      ("Major Line Color", Color)  = (0.40, 0.44, 0.52, 0.9)
        _MinorLineColor ("Minor Line Color", Color)  = (0.66, 0.69, 0.77, 0.55)
        _CellSize       ("Major Cell (m)", Float)    = 1.0
        _MajorWidth     ("Major Line (px)", Float)   = 1.0
        _MinorWidth     ("Minor Line (px)", Float)   = 0.7
        _Radius         ("Fade Radius (m)", Float)   = 9.0
        _Softness       ("Fade Softness (m)", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            // Colour blends normally, but the ALPHA channel uses (One, OneMinusSrcAlpha) so the
            // floor never lowers the eye-buffer alpha below the opaque VR background. Without this,
            // the radial edge-fade (alpha < 1) would punch holes that reveal the passthrough underlay
            // (the real room) in VR. dstA = srcA + dstA*(1-srcA): stays 1 in VR, and still fades to
            // (1 - MrAmount) in MR as the floor dissolves, so the passthrough crossfade is preserved.
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _LineColor;
                float4 _MinorLineColor;
                float  _CellSize;
                float  _MajorWidth;
                float  _MinorWidth;
                float  _Radius;
                float  _Softness;
            CBUFFER_END

            // Global, driven by the passthrough transition (0 = VR, 1 = MR). The grid is VR-only.
            float _AnkhoraMrAmount;

            // Anti-aliased line mask for a grid of the given cell size and pixel width.
            float GridLine(float2 worldXZ, float cell, float widthPx)
            {
                float2 coord  = worldXZ / max(cell, 1e-3);
                float2 deriv  = fwidth(coord);
                float2 toLine = abs(frac(coord - 0.5) - 0.5) / max(deriv, 1e-5);
                float  d      = min(toLine.x, toLine.y);
                return 1.0 - saturate(d - (widthPx - 1.0));
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 xz = IN.positionWS.xz;

                // Major lines at the cell size; minor lines every quarter cell -> 4x4 sub-squares.
                float major = GridLine(xz, _CellSize, _MajorWidth);
                float minor = GridLine(xz, _CellSize * 0.25, _MinorWidth);

                // Minor lines sit under the major ones; major wins where they overlap.
                half4 lineCol = lerp(_MinorLineColor, _LineColor, major);
                float lineMask = max(major, minor);

                half3 rgb = lerp(_BaseColor.rgb, lineCol.rgb, lineMask);
                float alpha = lerp(_BaseColor.a, lineCol.a, lineMask);

                // Circular fade centred on the player so the grid reads as infinite.
                float distXZ = distance(xz, GetCameraPositionWS().xz);
                float radial = 1.0 - smoothstep(_Radius - _Softness, _Radius, distXZ);

                alpha *= radial * (1.0 - saturate(_AnkhoraMrAmount));

                if (alpha < 0.002)
                    discard; // skip the fully-faded ring -> no overdraw beyond the circle

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
