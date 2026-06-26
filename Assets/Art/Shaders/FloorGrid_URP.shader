// Ankhora floor grid — URP Unlit, single-pass-instanced (stereo) safe.
// Anti-aliased world-space grid so the learner can read the space and their motion.
// Cheap on Quest: unlit, no textures, math-only lines via fwidth; fades with distance.
Shader "Ankhora/FloorGrid"
{
    Properties
    {
        _BaseColor   ("Base Color", Color)        = (0.92, 0.93, 0.95, 1)
        _LineColor   ("Line Color", Color)        = (0.45, 0.48, 0.55, 1)
        _CellSize    ("Cell Size (m)", Float)     = 0.5
        _LineWidth   ("Line Width (px)", Float)   = 1.2
        _FadeStart   ("Fade Start (m)", Float)    = 6.0
        _FadeEnd     ("Fade End (m)", Float)      = 16.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

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
                float  _CellSize;
                float  _LineWidth;
                float  _FadeStart;
                float  _FadeEnd;
            CBUFFER_END

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

                // Anti-aliased grid from world XZ: distance (in pixels) to the nearest cell line.
                float2 coord = IN.positionWS.xz / max(_CellSize, 1e-3);
                float2 deriv = fwidth(coord);
                float2 toLine = abs(frac(coord - 0.5) - 0.5) / max(deriv, 1e-5);
                float  lineDist = min(toLine.x, toLine.y);
                float  lineMask = 1.0 - saturate(lineDist - (_LineWidth - 1.0));

                // Fade lines out with distance from the camera so the far grid doesn't shimmer.
                float dist = distance(IN.positionWS, GetCameraPositionWS());
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, dist);

                half3 col = lerp(_BaseColor.rgb, _LineColor.rgb, lineMask * _LineColor.a * fade);
                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
