Shader "Ankhora/GhostHands_URP"
{
    // Soft "ghost hand" look (Ghost Hand Kit reference): a translucent, softly-lit body with a glowing
    // Fresnel rim and a gradient fade toward the wrist. ONE shader, fully recolourable (_FillColor /
    // _RimColor) so blue/gold/etc. are just material variants.
    //
    // Structure mirrors Meta's own Interaction/OculusHand shader (the proven approach on the runtime hand
    // mesh), which solves the two artefacts a naive single-pass transparent shader has on Quest:
    //   1. DEPTH PREPASS (ZWrite On, ColorMask 0) writes the front-most depth first; the colour pass then
    //      runs ZTest LEqual so only the nearest surface shades per pixel. Overlapping translucent layers
    //      (thumb over fingers) no longer compound their alpha, and you no longer see the BACK of the hand
    //      or the real room through the volume.
    //   2. WRIST FADE comes from VERTEX-COLOUR ALPHA, baked per-vertex from mesh geometry by
    //      Ankhora.Domain.Spatial.WristFadeGradient (0 at the wrist stump -> 1 across the hand). Two earlier
    //      attempts using Meta's UV glow-mask never faded on device (the runtime mesh's UVs could not be
    //      trusted); the geometry bake is deterministic and validated on the real mesh. If a mesh carries no
    //      vertex colours, COLOR defaults to opaque white -> the hand stays fully opaque (safe, not invisible).
    // Single-pass-instanced safe (stereo). No scene-colour / refraction (tiled mobile GPU).
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.42, 0.64, 1.0, 1.0)
        _RimColor ("Rim Glow Color", Color) = (0.88, 0.96, 1.0, 1.0)
        [Range(0,1)] _FillOpacity ("Fill Opacity", float) = 0.42
        [Range(0.25,6)] _RimPower ("Rim Power (soft<->tight)", float) = 2.0
        [Range(0,4)] _RimIntensity ("Rim Glow Intensity", float) = 1.9
        [Range(0,1)] _RimAlpha ("Rim Adds Opacity", float) = 0.7
        [Range(0,1)] _CoreBrightness ("Core Form Shading", float) = 0.4
        _LightDirection ("Soft Light Direction", Vector) = (0.3, 0.55, -0.75, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Back

        // --- Pass 1: depth prepass (no colour). Establishes the front-most surface so the fill below
        //     shades only the nearest layer. Untagged-equivalent LightMode "SRPDefaultUnlit" so URP runs it
        //     before UniversalForward, in shader order.
        Pass
        {
            Name "GhostDepth"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return 0;
            }
            ENDHLSL
        }

        // --- Pass 2: soft-glow translucent fill. Depth already written by Pass 1 -> ZTest LEqual, ZWrite Off.
        Pass
        {
            Name "GhostFill"
            Tags { "LightMode"="UniversalForward" }
            // Dual blend (matches Meta): straight alpha for colour, accumulate coverage in the alpha channel
            // so the result composites correctly if it ever lands in an overlay/eye-buffer with alpha.
            Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _RimColor;
                float _FillOpacity;
                float _RimPower;
                float _RimIntensity;
                float _RimAlpha;
                float _CoreBrightness;
                float4 _LightDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;         // alpha = baked wrist-fade gradient (0 wrist .. 1 hand)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                half wristFade : TEXCOORD2;   // vertex-colour alpha
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
                OUT.wristFade = IN.color.a;
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
                // Fade the bright rim glow by the SQUARED wrist gradient: a linear fade still left a faint
                // lit "reflection" on the dissolving stump, so attenuate it faster than the fill so the glow
                // is gone well before the silhouette does.
                half rimFade = IN.wristFade * IN.wristFade;
                half3 col = body + _RimColor.rgb * fres * _RimIntensity * rimFade;

                // Wrist gradient baked into vertex-colour alpha (0 at the stump), multiplies the whole alpha.
                half alpha = IN.wristFade * saturate(_FillOpacity + fres * _RimAlpha);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
