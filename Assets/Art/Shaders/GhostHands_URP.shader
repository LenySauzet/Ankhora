Shader "Ankhora/GhostHands_URP"
{
    // Soft "ghost hand" look (Ghost Hand Kit reference): a translucent, softly-lit body with a glowing
    // Fresnel rim and a gradient fade toward the wrist. URP Unlit-style, single-pass-instanced safe, no
    // scene-color/refraction. The wrist fade reuses Meta's baked hand gradient texture (_FingerGlowMask,
    // sampled on UV0) so it matches the native hand fade on the real runtime mesh.
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.25, 0.55, 1.0, 1.0)
        _RimColor ("Rim Glow Color", Color) = (0.7, 0.9, 1.0, 1.0)
        [Range(0,1)] _FillOpacity ("Fill Opacity", float) = 0.30
        [Range(0.25,6)] _RimPower ("Rim Power (soft<->tight)", float) = 2.5
        [Range(0,4)] _RimIntensity ("Rim Glow Intensity", float) = 1.6
        [Range(0,1)] _RimAlpha ("Rim Adds Opacity", float) = 0.7
        [Range(0,1)] _CoreBrightness ("Core Form Shading", float) = 0.5
        _LightDirection ("Soft Light Direction", Vector) = (0.3, 0.55, -0.75, 0)
        [NoScaleOffset] _FingerGlowMask ("Wrist Gradient (Meta mask)", 2D) = "white" {}
        [Range(0,1)] _WristFade ("Wrist Fade Bias", float) = 0.30
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "GhostBody"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FingerGlowMask);
            SAMPLER(sampler_FingerGlowMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _RimColor;
                float _FillOpacity;
                float _RimPower;
                float _RimIntensity;
                float _RimAlpha;
                float _CoreBrightness;
                float4 _LightDirection;
                float _WristFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = p.positionCS;
                OUT.normalWS = n.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(p.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float3 L = normalize(_LightDirection.xyz);

                // Soft "form" shading so raised areas read brighter (the lit, volumetric ghost look).
                half nl = saturate(dot(N, L) * 0.5 + 0.5);          // half-lambert, always >= 0
                half form = lerp(1.0, nl, _CoreBrightness);

                // Soft Fresnel rim glow on the silhouette.
                half fres = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);

                half3 body = _FillColor.rgb * form;
                half3 col = body + _RimColor.rgb * fres * _RimIntensity;

                // Wrist gradient from Meta's baked mask (UV0). At the wrist the mask alpha -> 0.
                half mask = SAMPLE_TEXTURE2D(_FingerGlowMask, sampler_FingerGlowMask, IN.uv).a;
                half fade = saturate(mask + _WristFade);

                half alpha = fade * saturate(_FillOpacity + fres * _RimAlpha);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
