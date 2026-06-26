// Ankhora gradient sky — URP procedural skybox, stereo-safe.
// A calming animated blob gradient (Ankhora blue #4B76F9 over light) that doubles as the
// VR<->MR transition: the ALPHA it writes drives a centre-of-vision radial reveal of the
// passthrough underlay. The skybox only paints background pixels (not opaque content), so
// the reveal never hides the model/grid. Driven by the global _AnkhoraMrAmount (0 = VR, 1 = MR).
// Cheap on Quest: fullscreen, no overdraw, no textures, a few soft blobs of math.
Shader "Ankhora/GradientSky"
{
    Properties
    {
        _ColorDeep  ("Deep Blue", Color) = (0.294, 0.463, 0.976, 1) // #4B76F9
        _ColorLight ("Light",     Color) = (0.96, 0.97, 1.0, 1)
        _BlobSpeed  ("Blob Speed", Float) = 0.06
        _Feather    ("Reveal Feather", Range(0.02, 0.6)) = 0.18
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AnkhoraReveal.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 viewDirWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorDeep;
                float4 _ColorLight;
                float  _BlobSpeed;
                float  _Feather;
            CBUFFER_END

            // Soft circular blob: 1 at the centre, smooth to 0 at radius r.
            float Blob(float2 p, float2 c, float r)
            {
                return saturate(1.0 - length(p - c) / max(r, 1e-3));
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.viewDirWS   = pos.positionWS - GetCameraPositionWS(); // skybox: dir from camera
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 dir = normalize(IN.viewDirWS);
                float t = _Time.y * _BlobSpeed;

                // Base vertical gradient: a touch lighter toward the top.
                float up = saturate(dir.y * 0.5 + 0.5);
                float3 col = lerp(_ColorDeep.rgb, _ColorLight.rgb, up * 0.30);

                // A few slow, soft light blobs drifting over the blue (project the direction to 2D).
                float2 uv = dir.xy * 1.6;
                float b = 0.0;
                b += Blob(uv, float2(sin(t * 1.3) * 0.6 - 0.3, cos(t * 1.1) * 0.4 + 0.25), 1.15);
                b += Blob(uv, float2(cos(t * 0.9) * 0.7 + 0.4, sin(t * 1.4) * 0.5 - 0.10), 0.95) * 0.85;
                b += Blob(uv, float2(sin(t * 0.7) * 0.5,        cos(t * 0.8) * 0.6 + 0.50), 1.30) * 0.6;
                col = lerp(col, _ColorLight.rgb, saturate(b) * 0.6);

                // Centre-of-vision radial reveal, shared with the floor grid (AnkhoraReveal.hlsl).
                // dir is already normalized -> use the N variant to skip a redundant normalize.
                float alpha = AnkhoraVrVisibilityN(dir, _Feather); // 1 = VR backdrop, 0 = passthrough

                // Premultiplied alpha: the XR compositor blends the underlay as
                // final = eyeColor + passthrough*(1 - alpha), so the sky colour must already be
                // scaled by alpha — otherwise it tints the real world where alpha -> 0.
                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
